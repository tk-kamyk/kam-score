using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Api.Endpoints;

public static class StandingsEndpoints
{
    public static RouteGroupBuilder MapStandingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}")
            .WithTags("Standings");

        group.MapGet("/standings", GetStandings);

        return group;
    }

    private static async Task<IResult> GetStandings(
        string tournamentId,
        string phaseId,
        string groupId,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        IGameRepository gameRepository,
        ITeamRepository teamRepository)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId);
        if (structure is null)
            throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.Phases.FirstOrDefault(p => p.Id == phaseId);
        if (phase is null)
            throw new NotFoundException(nameof(Phase), phaseId);

        var group = phase.Groups.FirstOrDefault(g => g.Id == groupId);
        if (group is null)
            throw new NotFoundException(nameof(Group), groupId);

        var games = (await gameRepository.GetByPhaseIdAsync(tournamentId, phaseId))
            .Where(g => g.GroupId == groupId)
            .ToList();

        var standings = StandingsCalculator.Calculate(phase.Format, games, group.TeamIds);

        var teams = (await teamRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var teamLookup = teams.ToDictionary(t => t.Id, t => t.Name);

        return Results.Ok(standings.Select(s => new StandingDto(
            s.TeamId,
            teamLookup.GetValueOrDefault(s.TeamId),
            s.Position,
            s.GamesPlayed,
            s.Wins,
            s.Draws,
            s.Losses,
            s.Points,
            s.SetsWon,
            s.SetsLost,
            s.SetDifference,
            s.PointsWon,
            s.PointsLost,
            s.PointDifference)));
    }
}
