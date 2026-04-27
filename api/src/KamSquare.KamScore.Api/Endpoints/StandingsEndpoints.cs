using FluentValidation;
using KamSquare.KamScore.Api.Helpers;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services.Formats;

namespace KamSquare.KamScore.Api.Endpoints;

public static class StandingsEndpoints
{
    public static RouteGroupBuilder MapStandingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}")
            .WithTags("Standings");

        group.MapGet("/standings", GetStandings);
        group.MapGet("/final-standings", GetFinalStandings);

        group.MapPut("/standings", UpdateManualStandings)
            .RequireAuthorization();

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

        var games = (await gameRepository.GetByPhaseIdAsync(tournamentId, phaseId))
            .Where(g => g.GroupId == groupId)
            .ToList();

        var standings = phase.CalculateGroupStandings(groupId, games);

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

    private static async Task<IResult> GetFinalStandings(
        string tournamentId,
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

        var lastPhase = structure.Phases.OrderByDescending(p => p.Order).FirstOrDefault();
        if (lastPhase is null || lastPhase.Status != PhaseStatus.Completed)
            return Results.Ok(new List<FinalStandingDto>());

        var gamesTask = gameRepository.GetByTournamentIdAsync(tournamentId);
        var teamsTask = teamRepository.GetByTournamentIdAsync(tournamentId);
        await Task.WhenAll(gamesTask, teamsTask);
        var allGames = (await gamesTask).Where(g => g.PhaseId == lastPhase.Id).ToList();
        var allTeams = (await teamsTask).ToList();

        var realTeamLookup = allTeams
            .Where(t => !t.IsPlaceholder)
            .ToDictionary(t => t.Id, t => t.Name);

        var strategy = PhaseFormatStrategy.For(lastPhase.Format);
        var result = new List<FinalStandingDto>();

        if (lastPhase.Levels.Count > 0)
        {
            foreach (var level in lastPhase.Levels.OrderBy(l => l.Order))
            {
                var levelGroups = lastPhase.Groups.Where(g => g.LevelId == level.Id).ToList();
                var ranked = RankGroups(levelGroups, allGames, strategy, realTeamLookup);
                AssignPositions(ranked, level.Name, result);
            }
        }
        else
        {
            var ranked = RankGroups(lastPhase.Groups, allGames, strategy, realTeamLookup);
            AssignPositions(ranked, null, result);
        }

        return Results.Ok(result);
    }

    private static List<(string TeamId, string TeamName)> RankGroups(
        IEnumerable<Group> groups,
        List<Game> phaseGames,
        IPhaseFormatStrategy strategy,
        Dictionary<string, string> realTeamLookup)
    {
        var groupStandings = groups
            .Select(g =>
            {
                var groupGames = phaseGames.Where(game => game.GroupId == g.Id).ToList();
                var realGroup = new Group
                {
                    Id = g.Id,
                    LevelId = g.LevelId,
                    TeamIds = g.TeamIds.Where(id => realTeamLookup.ContainsKey(id)).ToList(),
                    ManualStandingOrder = g.ManualStandingOrder.Where(id => realTeamLookup.ContainsKey(id)).ToList()
                };
                return strategy.CalculateStandings(groupGames, realGroup);
            })
            .SelectMany(s => s)
            .ToList();

        var ranked = strategy.RankCrossGroup(groupStandings);
        return ranked
            .Where(s => realTeamLookup.ContainsKey(s.TeamId))
            .Select(s => (s.TeamId, realTeamLookup[s.TeamId]))
            .ToList();
    }

    private static void AssignPositions(
        List<(string TeamId, string TeamName)> ranked,
        string? levelName,
        List<FinalStandingDto> result)
    {
        for (var i = 0; i < ranked.Count; i++)
        {
            result.Add(new FinalStandingDto(i + 1, ranked[i].TeamId, ranked[i].TeamName, levelName));
        }
    }

    private static async Task<IResult> UpdateManualStandings(
        string tournamentId,
        UpdateManualStandingsDto request,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        ITeamRepository teamRepository,
        ManualStandingsService manualStandingsService,
        IValidator<UpdateManualStandingsDto> validator,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var standings = await manualStandingsService.UpdateAsync(
            tournamentId, request.PhaseId, request.GroupId, request.OrderedTeamIds, structure);

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
