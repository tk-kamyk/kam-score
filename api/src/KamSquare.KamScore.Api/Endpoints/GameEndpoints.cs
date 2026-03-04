using System.Text.RegularExpressions;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Api.Helpers;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Api.Endpoints;

public static partial class GameEndpoints
{
    [GeneratedRegex("^[0-9A-Fa-f]{4}$")]
    private static partial Regex TournamentCodeRegex();

    public static RouteGroupBuilder MapGameEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}")
            .WithTags("Games");

        group.MapPost("/structure/phases/{phaseId}/generate-schedule", GenerateAndSchedule)
            .RequireAuthorization();
        group.MapGet("/games", GetGames);
        group.MapPut("/games/{gameId}/result", RecordResult);
        group.MapDelete("/structure/phases/{phaseId}/games", DeleteGames)
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GenerateAndSchedule(
        string tournamentId,
        string phaseId,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        ICourtRepository courtRepository,
        ITeamRepository teamRepository,
        IGameRepository gameRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        var tournament = await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);
        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);
        var phase = structure.GetPhase(phaseId);
        var courts = (await courtRepository.GetByTournamentIdAsync(tournamentId)).ToList();

        // Determine if this is a placeholder generation (phase 2+ with no teams assigned)
        var hasTeamsAssigned = phase.Groups.Any(g => g.TeamIds.Count > 0);
        var isPlaceholderGeneration = phase.Order > 1 && !hasTeamsAssigned;

        await ValidateGenerationPrerequisitesAsync(
            tournament, phase, courts, tournamentId, phaseId, gameRepository, isPlaceholderGeneration, structure);

        List<Game> allGames;
        if (isPlaceholderGeneration)
        {
            var previousPhase = structure.GetPreviousPhase(phaseId)!;
            var totalTeams = PhaseAdvancementCalculator.GetExpectedTeamCount(previousPhase)
                ?? throw new ValidationException(
                    [new ValidationFailure("Progression", "Previous phase must have GroupWinners or TotalTeamsProceeding configured.")]);

            allGames = CrossPhaseGameGenerator.GenerateWithPlaceholders(
                tournamentId, phaseId, phase, previousPhase.Name, totalTeams);
        }
        else
        {
            allGames = GenerateGamesForPhase(phase, tournamentId, phaseId);
        }

        var courtIds = courts.Select(c => c.Id).ToList();
        var startDateTime = tournament.StartTime?.Date.Add(phase.StartTime!.Value.ToTimeSpan())
            ?? DateTime.Today.Add(phase.StartTime!.Value.ToTimeSpan());
        GameScheduler.Schedule(allGames, courtIds, startDateTime, tournament.GameLength!.Value);

        var savedGames = (await gameRepository.CreateBatchAsync(allGames)).ToList();

        // Activate phase 1 when games are generated (New → InProgress)
        if (phase.Order == 1 && phase.Status == PhaseStatus.New)
        {
            structure.ActivatePhase(phaseId);
            await structureRepository.UpdateAsync(structure);
        }

        var teams = (await teamRepository.GetByTournamentIdAsync(tournamentId)).ToList();

        return Results.Created(
            $"/api/tournaments/{tournamentId}/games?phaseId={phaseId}",
            EnrichGamesWithNames(savedGames, teams, courts, mapper));
    }

    private static async Task<IResult> GetGames(
        string tournamentId,
        string? phaseId,
        string? groupId,
        string? courtId,
        ITournamentRepository tournamentRepository,
        IGameRepository gameRepository,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        IMapper mapper)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        var games = phaseId is not null
            ? await gameRepository.GetByPhaseIdAsync(tournamentId, phaseId)
            : await gameRepository.GetByTournamentIdAsync(tournamentId);

        var gameList = games.ToList();
        if (groupId is not null)
            gameList = gameList.Where(g => g.GroupId == groupId).ToList();
        if (courtId is not null)
            gameList = gameList.Where(g => g.CourtId == courtId).ToList();

        var teams = (await teamRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var courts = (await courtRepository.GetByTournamentIdAsync(tournamentId)).ToList();

        return Results.Ok(EnrichGamesWithNames(gameList.OrderBy(g => g.StartTime), teams, courts, mapper));
    }

    private static async Task<IResult> DeleteGames(
        string tournamentId,
        string phaseId,
        ITournamentRepository tournamentRepository,
        IGameRepository gameRepository,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        await gameRepository.DeleteByPhaseIdAsync(tournamentId, phaseId);

        return Results.NoContent();
    }

    private static async Task<IResult> RecordResult(
        string tournamentId,
        string gameId,
        GameResultDto resultDto,
        ITournamentRepository tournamentRepository,
        IGameRepository gameRepository,
        ICurrentUserService currentUser,
        HttpContext httpContext,
        IMapper mapper)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        var isOwner = currentUser.IsAuthenticated && tournament.IsOwnedBy(currentUser.UserId!);
        if (!isOwner)
        {
            var code = httpContext.Request.Headers["X-Tournament-Code"].FirstOrDefault();
            if (code is null)
            {
                if (!currentUser.IsAuthenticated)
                    throw new UnauthorizedException("Authentication required.");
                throw new ForbiddenException();
            }
            if (!TournamentCodeRegex().IsMatch(code))
                throw new ForbiddenException();
            if (!tournament.TournamentCode.Equals(code, StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException();
        }

        var game = await gameRepository.GetByIdAsync(tournamentId, gameId);
        if (game is null)
            throw new NotFoundException(nameof(Game), gameId);

        if (resultDto.Sets is { Count: > 0 })
        {
            var sets = resultDto.Sets
                .Select(s => new SetResult(s.HomePoints, s.AwayPoints))
                .ToList();
            game.RecordResult(sets);
        }
        else
        {
            game.RecordSimpleResult(resultDto.HomeScore!.Value, resultDto.AwayScore!.Value);
        }

        var updatedGame = await gameRepository.UpdateAsync(game);

        if (updatedGame.Label is not null)
        {
            var allGames = (await gameRepository.GetByPhaseIdAsync(tournamentId, updatedGame.PhaseId))
                .Where(g => g.GroupId == updatedGame.GroupId)
                .ToList();

            var advancedGames = BracketUtilities.ResolveAdvancement(updatedGame, allGames);
            foreach (var advancedGame in advancedGames)
            {
                await gameRepository.UpdateAsync(advancedGame);
            }
        }

        return Results.Ok(mapper.Map<GameDto>(updatedGame));
    }

    private static async Task ValidateGenerationPrerequisitesAsync(
        Tournament tournament, Phase phase, List<Court> courts,
        string tournamentId, string phaseId,
        IGameRepository gameRepository, bool isPlaceholderGeneration = false,
        TournamentStructure? structure = null)
    {
        if (tournament.GameLength is null or <= 0)
            throw new ValidationException(
                [new ValidationFailure("GameLength", "Tournament must have a game length configured.")]);

        if (phase.StartTime is null)
            throw new ValidationException(
                [new ValidationFailure("StartTime", "Phase must have a start time configured.")]);

        if (courts.Count == 0)
            throw new ValidationException(
                [new ValidationFailure("Courts", "At least one court is required.")]);

        if (isPlaceholderGeneration)
        {
            if (phase.Groups.Count == 0)
                throw new ValidationException(
                    [new ValidationFailure("Groups", "Phase must have at least one group.")]);

            var previousPhase = structure?.GetPreviousPhase(phaseId);
            if (previousPhase is null ||
                (previousPhase.GroupWinners is null && previousPhase.TotalTeamsProceeding is null))
                throw new ValidationException(
                    [new ValidationFailure("Progression", "Previous phase must have GroupWinners or TotalTeamsProceeding configured.")]);
        }
        else
        {
            if (phase.Groups.Count == 0 || phase.Groups.All(g => g.TeamIds.Count == 0))
                throw new ValidationException(
                    [new ValidationFailure("Teams", "Phase groups must have teams assigned.")]);
        }

        if (await gameRepository.GamesExistForPhaseAsync(tournamentId, phaseId))
            throw new ValidationException(
                [new ValidationFailure("Games", "Games already exist for this phase. Delete them first.")]);
    }

    private static List<Game> GenerateGamesForPhase(Phase phase, string tournamentId, string phaseId)
    {
        var allGames = new List<Game>();
        foreach (var group in phase.Groups)
        {
            if (group.TeamIds.Count <= 1) continue;

            var games = phase.Format switch
            {
                PhaseFormat.RoundRobin => RoundRobinGenerator.Generate(
                    tournamentId, phaseId, group.Id, group.TeamIds),
                PhaseFormat.PlayoffElimination => PlayoffEliminationGenerator.Generate(
                    tournamentId, phaseId, group.Id, group.TeamIds),
                PhaseFormat.PlayoffWithPlacement => PlayoffWithPlacementGenerator.Generate(
                    tournamentId, phaseId, group.Id, group.TeamIds),
                _ => throw new ValidationException(
                    [new ValidationFailure("Format", $"Unsupported phase format: {phase.Format}")])
            };

            allGames.AddRange(games);
        }

        if (allGames.Count == 0)
            throw new ValidationException(
                [new ValidationFailure("Games", "No games could be generated. Check team assignments.")]);

        return allGames;
    }

    private static List<GameDto> EnrichGamesWithNames(
        IEnumerable<Game> games, List<Team> teams, List<Court> courts, IMapper mapper)
    {
        var teamLookup = teams.ToDictionary(t => t.Id, t => t.Name);
        var courtLookup = courts.ToDictionary(c => c.Id, c => c.Name);

        return games.Select(g =>
        {
            var dto = mapper.Map<GameDto>(g);
            return dto with
            {
                HomeTeamName = g.HomeTeamId is not null && teamLookup.TryGetValue(g.HomeTeamId, out var hn) ? hn : null,
                AwayTeamName = g.AwayTeamId is not null && teamLookup.TryGetValue(g.AwayTeamId, out var an) ? an : null,
                RefereeTeamName = g.RefereeTeamId is not null && teamLookup.TryGetValue(g.RefereeTeamId, out var rn) ? rn : null,
                CourtName = g.CourtId is not null && courtLookup.TryGetValue(g.CourtId, out var cn) ? cn : null
            };
        }).ToList();
    }
}
