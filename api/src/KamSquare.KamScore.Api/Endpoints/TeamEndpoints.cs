using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Api.Helpers;
using FluentValidation;
using FluentValidation.Results;

namespace KamSquare.KamScore.Api.Endpoints;

public static class TeamEndpoints
{
    public static RouteGroupBuilder MapTeamEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}/teams")
            .WithTags("Teams");

        group.MapGet("/", GetTeams);
        group.MapPost("/", CreateTeam).RequireAuthorization();
        group.MapPut("/{teamId}", UpdateTeam).RequireAuthorization();
        group.MapDelete("/{teamId}", DeleteTeam).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetTeams(
        string tournamentId,
        bool? includePlaceholders,
        ITeamRepository teamRepository,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        var teams = await teamRepository.GetByTournamentIdAsync(tournamentId);
        if (includePlaceholders is not true)
            teams = teams.Where(t => !t.IsPlaceholder);

        var dtos = mapper.Map<IEnumerable<TeamDto>>(teams);

        if (!currentUser.IsAuthenticated || !tournament.IsOwnedBy(currentUser.UserId!))
            dtos = dtos.Select(HideContactInfo);

        return Results.Ok(dtos);
    }

    private static async Task<IResult> CreateTeam(
        string tournamentId,
        TeamDto request,
        ITeamRepository teamRepository,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        if (await teamRepository.ExistsByNameAsync(tournamentId, request.Name))
            throw new ValidationException(
                [new ValidationFailure("Name", $"A team with name '{request.Name}' already exists in this tournament.")]);

        var team = Team.Create(request.Name, request.Level, tournamentId, request.Email, request.Phone);
        var created = await teamRepository.CreateAsync(team);
        var dto = mapper.Map<TeamDto>(created);

        return Results.Created($"/api/tournaments/{tournamentId}/teams/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateTeam(
        string tournamentId,
        string teamId,
        TeamDto request,
        ITeamRepository teamRepository,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var team = await teamRepository.GetByIdAsync(teamId, tournamentId);
        if (team is null)
            throw new NotFoundException(nameof(Team), teamId);

        if (await teamRepository.ExistsByNameAsync(tournamentId, request.Name, teamId))
            throw new ValidationException(
                [new ValidationFailure("Name", $"A team with name '{request.Name}' already exists in this tournament.")]);

        team.Update(request.Name, request.Level, request.Email, request.Phone);
        var updated = await teamRepository.UpdateAsync(team);
        var dto = mapper.Map<TeamDto>(updated);

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteTeam(
        string tournamentId,
        string teamId,
        ITeamRepository teamRepository,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        IGameRepository gameRepository,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var team = await teamRepository.GetByIdAsync(teamId, tournamentId);
        if (team is null)
            throw new NotFoundException(nameof(Team), teamId);

        // Check if team is assigned to any phase group
        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId);
        if (structure is not null)
        {
            var assignedInPhase = structure.Phases
                .FirstOrDefault(p => p.Groups.Any(g => g.HasTeam(teamId)));
            if (assignedInPhase is not null)
                throw new ReferentialIntegrityException("team", team.Name,
                    $"team is assigned to a group in phase '{assignedInPhase.Name}'. Remove the team assignment first");
        }

        // Check if team is referenced in any games
        if (await gameRepository.TeamIsReferencedInGamesAsync(tournamentId, teamId))
            throw new ReferentialIntegrityException("team", team.Name,
                "team is referenced in games. Delete the related games first");

        await teamRepository.DeleteAsync(teamId, tournamentId);

        return Results.NoContent();
    }

    private static TeamDto HideContactInfo(TeamDto dto)
    {
        return dto with { Email = null, Phone = null };
    }
}
