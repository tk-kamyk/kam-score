using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Api.Helpers;
using FluentValidation;
using FluentValidation.Results;

namespace KamSquare.KamScore.Api.Endpoints;

public static class TeamAssignmentEndpoints
{
    public static RouteGroupBuilder MapTeamAssignmentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}/structure/phases/{phaseId}/groups/{groupId}/teams")
            .WithTags("Team Assignments");

        group.MapPost("/", AssignTeam).RequireAuthorization();
        group.MapDelete("/{teamId}", RemoveTeam).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> AssignTeam(
        string tournamentId,
        string phaseId,
        string groupId,
        TeamAssignmentRequest request,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        ITeamRepository teamRepository,
        PhaseGuardService phaseGuardService,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);
        await phaseGuardService.EnsureStructureEditableAsync(phase, tournamentId);

        var team = await teamRepository.GetByIdAsync(request.TeamId, tournamentId);
        if (team is null)
            throw new NotFoundException(nameof(Team), request.TeamId);

        if (structure.TeamExistsInPhase(phaseId, request.TeamId))
            throw new ValidationException(
                [new ValidationFailure("TeamId", "Team is already assigned to a group in this phase.")]);

        structure.AssignTeam(phaseId, groupId, request.TeamId);
        await structureRepository.UpdateAsync(structure);

        return Results.Created(
            $"/api/tournaments/{tournamentId}/structure/phases/{phaseId}/groups/{groupId}/teams/{request.TeamId}",
            new { TeamId = request.TeamId });
    }

    private static async Task<IResult> RemoveTeam(
        string tournamentId,
        string phaseId,
        string groupId,
        string teamId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        PhaseGuardService phaseGuardService,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);
        await phaseGuardService.EnsureStructureEditableAsync(phase, tournamentId);

        structure.RemoveTeam(phaseId, groupId, teamId);
        await structureRepository.UpdateAsync(structure);

        return Results.NoContent();
    }
}

public record TeamAssignmentRequest(string TeamId);
