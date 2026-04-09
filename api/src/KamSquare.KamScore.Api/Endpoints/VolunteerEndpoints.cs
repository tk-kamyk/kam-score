using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Api.Helpers;
using FluentValidation;
using FluentValidation.Results;

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

        return group;
    }

    private static async Task<IResult> GetVolunteers(
        string tournamentId,
        IVolunteerRepository volunteerRepository,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        if (!TournamentAuthorizationHelper.HasAdminAccess(tournament, currentUser))
            throw new Application.Exceptions.ForbiddenException();

        var volunteers = await volunteerRepository.GetByTournamentIdAsync(tournamentId);
        var dtos = mapper.Map<IEnumerable<VolunteerDto>>(volunteers);

        return Results.Ok(dtos);
    }

    private static async Task<IResult> CreateVolunteer(
        string tournamentId,
        VolunteerDto request,
        IVolunteerRepository volunteerRepository,
        ITournamentRepository tournamentRepository,
        ITeamRepository teamRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        if (await volunteerRepository.ExistsByNameAsync(tournamentId, request.Name))
            throw new ValidationException(
                [new ValidationFailure("Name", $"A volunteer with name '{request.Name}' already exists in this tournament.")]);

        if (request.TeamId is not null)
        {
            var team = await teamRepository.GetByIdAsync(request.TeamId, tournamentId);
            if (team is null)
                throw new ValidationException(
                    [new ValidationFailure("TeamId", $"Team '{request.TeamId}' does not exist in this tournament.")]);
        }

        var volunteer = Volunteer.Create(request.Name, tournamentId, request.Contact, request.TeamId);
        var created = await volunteerRepository.CreateAsync(volunteer);
        var dto = mapper.Map<VolunteerDto>(created);

        return Results.Created($"/api/tournaments/{tournamentId}/volunteers/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateVolunteer(
        string tournamentId,
        string volunteerId,
        VolunteerDto request,
        IVolunteerRepository volunteerRepository,
        ITournamentRepository tournamentRepository,
        ITeamRepository teamRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var volunteer = await volunteerRepository.GetByIdAsync(volunteerId, tournamentId);
        if (volunteer is null)
            throw new NotFoundException(nameof(Volunteer), volunteerId);

        if (await volunteerRepository.ExistsByNameAsync(tournamentId, request.Name, volunteerId))
            throw new ValidationException(
                [new ValidationFailure("Name", $"A volunteer with name '{request.Name}' already exists in this tournament.")]);

        if (request.TeamId is not null)
        {
            var team = await teamRepository.GetByIdAsync(request.TeamId, tournamentId);
            if (team is null)
                throw new ValidationException(
                    [new ValidationFailure("TeamId", $"Team '{request.TeamId}' does not exist in this tournament.")]);
        }

        volunteer.Update(request.Name, request.Contact, request.TeamId);
        var updated = await volunteerRepository.UpdateAsync(volunteer);
        var dto = mapper.Map<VolunteerDto>(updated);

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteVolunteer(
        string tournamentId,
        string volunteerId,
        IVolunteerRepository volunteerRepository,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var volunteer = await volunteerRepository.GetByIdAsync(volunteerId, tournamentId);
        if (volunteer is null)
            throw new NotFoundException(nameof(Volunteer), volunteerId);

        await volunteerRepository.DeleteAsync(volunteerId, tournamentId);

        return Results.NoContent();
    }

    // --- Shift Endpoints ---

    private static async Task<List<ShiftGroup>> GetShiftGroups(
        Tournament tournament,
        ITournamentStructureRepository structureRepository,
        IGameRepository gameRepository)
    {
        var structure = await structureRepository.GetByTournamentIdAsync(tournament.Id);
        var phases = structure?.Phases?.OrderBy(p => p.Order).ToList() ?? [];

        if (phases.Count == 0)
            return VolunteerShiftCalculator.CalculateShiftGroups([], tournament.GameLength);

        var allGames = (await gameRepository.GetByTournamentIdAsync(tournament.Id)).ToList();

        var phaseInfos = phases.Select(p =>
        {
            var phaseGames = allGames.Where(g => g.PhaseId == p.Id).ToList();
            var maxRounds = phaseGames.Count > 0 ? phaseGames.Max(g => g.Round) : 1;
            return (p.Name, p.StartTime, maxRounds);
        }).ToList();

        return VolunteerShiftCalculator.CalculateShiftGroups(phaseInfos, tournament.GameLength);
    }

    private static bool IsTeamBusyAtTime(string? teamId, List<Game> allGames, TimeOnly shiftTime)
    {
        if (teamId is null) return false;
        return allGames.Any(g =>
            g.StartTime.HasValue &&
            TimeOnly.FromDateTime(g.StartTime.Value) == shiftTime &&
            (g.HomeTeamId == teamId || g.AwayTeamId == teamId || g.RefereeTeamId == teamId));
    }

    private static async Task<IResult> GetShifts(
        string tournamentId,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        IVolunteerRepository volunteerRepository,
        IGameRepository gameRepository,
        ICurrentUserService currentUser)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);
        if (!TournamentAuthorizationHelper.HasAdminAccess(tournament, currentUser))
            throw new Application.Exceptions.ForbiddenException();

        var shiftGroups = await GetShiftGroups(tournament, structureRepository, gameRepository);
        var volunteers = (await volunteerRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var allGames = (await gameRepository.GetByTournamentIdAsync(tournamentId)).ToList();

        var response = shiftGroups.Select(group => new ShiftGroupResponseDto(
            group.Name,
            group.IsSpecial,
            group.Shifts.Select(shiftTime =>
            {
                var assignedVols = volunteers
                    .Where(v => v.Assignments.Any(a => a.ShiftGroup == group.Name && a.ShiftTime == shiftTime))
                    .Select(v => new ShiftVolunteerDto(
                        v.Id,
                        v.Name,
                        group.IsSpecial || shiftTime is null || !IsTeamBusyAtTime(v.TeamId, allGames, shiftTime.Value)))
                    .ToList();

                return new ShiftSlotDto(
                    shiftTime?.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    assignedVols);
            }).ToList()
        )).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetAvailableVolunteers(
        string tournamentId,
        string shiftGroup,
        string shiftTime,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        IVolunteerRepository volunteerRepository,
        IGameRepository gameRepository,
        ICurrentUserService currentUser)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);
        if (!TournamentAuthorizationHelper.HasAdminAccess(tournament, currentUser))
            throw new Application.Exceptions.ForbiddenException();

        if (!TimeOnly.TryParse(shiftTime, out var parsedTime))
            throw new ValidationException(
                [new ValidationFailure("shiftTime", $"Invalid time format: '{shiftTime}'.")]);

        var volunteers = (await volunteerRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var allGames = (await gameRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var gameLengthMinutes = tournament.GameLength ?? 0;

        var previousTime = gameLengthMinutes > 0 ? parsedTime.AddMinutes(-gameLengthMinutes) : (TimeOnly?)null;
        var nextTime = gameLengthMinutes > 0 ? parsedTime.AddMinutes(gameLengthMinutes) : (TimeOnly?)null;

        var result = volunteers.Select(v =>
        {
            var available = !IsTeamBusyAtTime(v.TeamId, allGames, parsedTime);
            var playsBefore = previousTime.HasValue && IsTeamBusyAtTime(v.TeamId, allGames, previousTime.Value);
            var playsAfter = nextTime.HasValue && IsTeamBusyAtTime(v.TeamId, allGames, nextTime.Value);
            var assigned = v.Assignments.Any(a => a.ShiftGroup == shiftGroup && a.ShiftTime == parsedTime);

            return new VolunteerAvailabilityDto(
                v.Id, v.Name, v.Assignments.Count, available, playsBefore, playsAfter, assigned);
        })
        .OrderByDescending(v => v.Available)
        .ThenBy(v => v.ShiftCount)
        .ThenBy(v => v.Name)
        .ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetAvailableVolunteersSpecial(
        string tournamentId,
        string shiftGroup,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        IVolunteerRepository volunteerRepository,
        ICurrentUserService currentUser)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);
        if (!TournamentAuthorizationHelper.HasAdminAccess(tournament, currentUser))
            throw new Application.Exceptions.ForbiddenException();

        var volunteers = (await volunteerRepository.GetByTournamentIdAsync(tournamentId)).ToList();

        var result = volunteers.Select(v =>
        {
            var assigned = v.Assignments.Any(a => a.ShiftGroup == shiftGroup && a.ShiftTime is null);
            return new VolunteerAvailabilityDto(
                v.Id, v.Name, v.Assignments.Count, true, false, false, assigned);
        })
        .OrderBy(v => v.ShiftCount)
        .ThenBy(v => v.Name)
        .ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> AssignVolunteerToShift(
        string tournamentId,
        string shiftGroup,
        string shiftTime,
        string volunteerId,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        IVolunteerRepository volunteerRepository,
        IGameRepository gameRepository,
        ICurrentUserService currentUser)
    {
        var tournament = await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        if (!TimeOnly.TryParse(shiftTime, out var parsedTime))
            throw new ValidationException(
                [new ValidationFailure("shiftTime", $"Invalid time format: '{shiftTime}'.")]);

        var shiftGroups = await GetShiftGroups(tournament, structureRepository, gameRepository);
        var group = shiftGroups.FirstOrDefault(g => g.Name == shiftGroup);
        if (group is null || !group.Shifts.Contains(parsedTime))
            throw new ValidationException(
                [new ValidationFailure("shiftTime", $"'{shiftTime}' is not a valid shift time for '{shiftGroup}'.")]);

        var volunteer = await volunteerRepository.GetByIdAsync(volunteerId, tournamentId);
        if (volunteer is null)
            throw new NotFoundException(nameof(Volunteer), volunteerId);

        volunteer.AssignShift(shiftGroup, parsedTime);
        await volunteerRepository.UpdateAsync(volunteer);

        return Results.Ok();
    }

    private static async Task<IResult> AssignVolunteerToSpecialShift(
        string tournamentId,
        string shiftGroup,
        string volunteerId,
        ITournamentRepository tournamentRepository,
        IVolunteerRepository volunteerRepository,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        if (shiftGroup is not ("Set-up" or "Cleanup"))
            throw new ValidationException(
                [new ValidationFailure("shiftGroup", $"'{shiftGroup}' is not a valid special shift group.")]);

        var volunteer = await volunteerRepository.GetByIdAsync(volunteerId, tournamentId);
        if (volunteer is null)
            throw new NotFoundException(nameof(Volunteer), volunteerId);

        volunteer.AssignShift(shiftGroup, null);
        await volunteerRepository.UpdateAsync(volunteer);

        return Results.Ok();
    }

    private static async Task<IResult> UnassignVolunteerFromShift(
        string tournamentId,
        string shiftGroup,
        string shiftTime,
        string volunteerId,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        IVolunteerRepository volunteerRepository,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        if (!TimeOnly.TryParse(shiftTime, out var parsedTime))
            throw new ValidationException(
                [new ValidationFailure("shiftTime", $"Invalid time format: '{shiftTime}'.")]);

        var volunteer = await volunteerRepository.GetByIdAsync(volunteerId, tournamentId);
        if (volunteer is null)
            throw new NotFoundException(nameof(Volunteer), volunteerId);

        volunteer.UnassignShift(shiftGroup, parsedTime);
        await volunteerRepository.UpdateAsync(volunteer);

        return Results.Ok();
    }

    private static async Task<IResult> UnassignVolunteerFromSpecialShift(
        string tournamentId,
        string shiftGroup,
        string volunteerId,
        ITournamentRepository tournamentRepository,
        IVolunteerRepository volunteerRepository,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var volunteer = await volunteerRepository.GetByIdAsync(volunteerId, tournamentId);
        if (volunteer is null)
            throw new NotFoundException(nameof(Volunteer), volunteerId);

        volunteer.UnassignShift(shiftGroup, null);
        await volunteerRepository.UpdateAsync(volunteer);

        return Results.Ok();
    }
}
