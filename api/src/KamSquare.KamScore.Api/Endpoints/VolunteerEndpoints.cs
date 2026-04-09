using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;
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
}
