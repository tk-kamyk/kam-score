using AutoMapper;
using KamSquare.KamScore.Api.Helpers;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;

namespace KamSquare.KamScore.Api.Endpoints;

public static class VolunteerEndpoints
{
    public static RouteGroupBuilder MapVolunteerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}/volunteers")
            .WithTags("Volunteers")
            .RequireAuthorization();

        group.MapGet("/", GetVolunteers);
        group.MapPost("/", CreateVolunteer);
        group.MapPut("/{volunteerId}", UpdateVolunteer);
        group.MapDelete("/{volunteerId}", DeleteVolunteer);

        group.MapGet("/shifts", GetShifts);
        group.MapGet("/shifts/{shiftGroup}/{shiftTime}/available", GetAvailableVolunteers);
        group.MapGet("/shifts/{shiftGroup}/available", GetAvailableVolunteersSpecial);
        group.MapPost("/shifts/{shiftGroup}/{shiftTime}/assign/{volunteerId}", AssignVolunteerToShift);
        group.MapPost("/shifts/{shiftGroup}/assign/{volunteerId}", AssignVolunteerToSpecialShift);
        group.MapDelete("/shifts/{shiftGroup}/{shiftTime}/assign/{volunteerId}", UnassignVolunteerFromShift);
        group.MapDelete("/shifts/{shiftGroup}/assign/{volunteerId}", UnassignVolunteerFromSpecialShift);

        group.MapDelete("/shifts/{shiftGroup}/assignments", ClearShiftGroupAssignments);
        group.MapPost("/shifts/{shiftGroup}/auto-assign", AutoAssignShiftGroup);

        return group;
    }

    private static async Task<IResult> GetVolunteers(
        string tournamentId,
        IVolunteerRepository volunteerRepository,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await EnsureAdminAccessAsync(tournamentRepository, tournamentId, currentUser);

        var volunteers = await volunteerRepository.GetByTournamentIdAsync(tournamentId);
        return Results.Ok(mapper.Map<IEnumerable<VolunteerDto>>(volunteers));
    }

    private static async Task<IResult> CreateVolunteer(
        string tournamentId,
        VolunteerDto request,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var created = await volunteerService.CreateAsync(tournamentId, request.Name, request.Contact, request.TeamId);
        var dto = mapper.Map<VolunteerDto>(created);

        return Results.Created($"/api/tournaments/{tournamentId}/volunteers/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateVolunteer(
        string tournamentId,
        string volunteerId,
        VolunteerDto request,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var updated = await volunteerService.UpdateAsync(tournamentId, volunteerId, request.Name, request.Contact, request.TeamId);
        return Results.Ok(mapper.Map<VolunteerDto>(updated));
    }

    private static async Task<IResult> DeleteVolunteer(
        string tournamentId,
        string volunteerId,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        await volunteerService.DeleteAsync(tournamentId, volunteerId);
        return Results.NoContent();
    }

    // --- Shifts ---

    private static async Task<IResult> GetShifts(
        string tournamentId,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser)
    {
        var tournament = await EnsureAdminAccessAsync(tournamentRepository, tournamentId, currentUser);

        var shifts = await volunteerService.GetShiftsAsync(tournament);
        return Results.Ok(shifts);
    }

    private static async Task<IResult> GetAvailableVolunteers(
        string tournamentId,
        string shiftGroup,
        string shiftTime,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser)
    {
        var tournament = await EnsureAdminAccessAsync(tournamentRepository, tournamentId, currentUser);

        var result = await volunteerService.GetAvailableVolunteersAsync(tournament, shiftGroup, shiftTime);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAvailableVolunteersSpecial(
        string tournamentId,
        string shiftGroup,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser)
    {
        await EnsureAdminAccessAsync(tournamentRepository, tournamentId, currentUser);

        var result = await volunteerService.GetAvailableVolunteersForSpecialAsync(tournamentId, shiftGroup);
        return Results.Ok(result);
    }

    private static async Task<IResult> AssignVolunteerToShift(
        string tournamentId,
        string shiftGroup,
        string shiftTime,
        string volunteerId,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        AssignShiftRequestDto? request = null)
    {
        var tournament = await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        await volunteerService.AssignToShiftAsync(
            tournament, shiftGroup, shiftTime, volunteerId,
            request is not null ? StationChange.Set(request.Station) : StationChange.None);
        return Results.Ok();
    }

    private static async Task<IResult> AssignVolunteerToSpecialShift(
        string tournamentId,
        string shiftGroup,
        string volunteerId,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        AssignShiftRequestDto? request = null)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        await volunteerService.AssignToSpecialShiftAsync(
            tournamentId, shiftGroup, volunteerId,
            request is not null ? StationChange.Set(request.Station) : StationChange.None);
        return Results.Ok();
    }

    private static async Task<IResult> UnassignVolunteerFromShift(
        string tournamentId,
        string shiftGroup,
        string shiftTime,
        string volunteerId,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        await volunteerService.UnassignFromShiftAsync(tournamentId, shiftGroup, shiftTime, volunteerId);
        return Results.Ok();
    }

    private static async Task<IResult> UnassignVolunteerFromSpecialShift(
        string tournamentId,
        string shiftGroup,
        string volunteerId,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        await volunteerService.UnassignFromSpecialShiftAsync(tournamentId, shiftGroup, volunteerId);
        return Results.Ok();
    }

    // --- Bulk shift-group operations ---

    private static async Task<IResult> ClearShiftGroupAssignments(
        string tournamentId,
        string shiftGroup,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser)
    {
        var tournament = await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        await volunteerService.ClearShiftGroupAssignmentsAsync(tournament, shiftGroup);
        return Results.NoContent();
    }

    private static async Task<IResult> AutoAssignShiftGroup(
        string tournamentId,
        string shiftGroup,
        AutoAssignShiftGroupDto request,
        VolunteerService volunteerService,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser)
    {
        var tournament = await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        await volunteerService.AutoAssignShiftGroupAsync(tournament, shiftGroup, request.VolunteersPerShift, request.StationCount);
        return Results.Ok();
    }

    private static async Task<Tournament> EnsureAdminAccessAsync(
        ITournamentRepository tournamentRepository,
        string tournamentId,
        ICurrentUserService currentUser)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(Tournament), tournamentId);

        if (!TournamentAuthorizationHelper.HasAdminAccess(tournament, currentUser))
            throw new Application.Exceptions.ForbiddenException();

        return tournament;
    }
}
