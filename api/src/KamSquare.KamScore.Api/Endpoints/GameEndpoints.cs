using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Api.Endpoints;

public static class GameEndpoints
{
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
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        if (!tournament.IsOwnedBy(currentUser.UserId!))
            throw new ForbiddenException();

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);

        // Validate prerequisites
        if (tournament.GameLength is null or <= 0)
            throw new ValidationException(
                [new ValidationFailure("GameLength", "Tournament must have a game length configured.")]);

        if (phase.StartTime is null)
            throw new ValidationException(
                [new ValidationFailure("StartTime", "Phase must have a start time configured.")]);

        var courts = (await courtRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        if (courts.Count == 0)
            throw new ValidationException(
                [new ValidationFailure("Courts", "At least one court is required.")]);

        if (phase.Groups.Count == 0 || phase.Groups.All(g => g.TeamIds.Count == 0))
            throw new ValidationException(
                [new ValidationFailure("Teams", "Phase groups must have teams assigned.")]);

        if (await gameRepository.GamesExistForPhaseAsync(tournamentId, phaseId))
            throw new ValidationException(
                [new ValidationFailure("Games", "Games already exist for this phase. Delete them first.")]);

        // Generate games for each group based on phase format
        var allGames = new List<Game>();
        foreach (var group in phase.Groups)
        {
            if (group.TeamIds.Count <= 1)
                continue;

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

        // Schedule games across courts and time slots
        var courtIds = courts.Select(c => c.Id).ToList();
        var startDateTime = tournament.StartTime?.Date.Add(phase.StartTime.Value.ToTimeSpan())
            ?? DateTime.Today.Add(phase.StartTime.Value.ToTimeSpan());

        GameScheduler.Schedule(allGames, courtIds, startDateTime, tournament.GameLength.Value);

        // Save all games
        var savedGames = (await gameRepository.CreateBatchAsync(allGames)).ToList();

        // Resolve team and court names for response
        var teams = (await teamRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var teamLookup = teams.ToDictionary(t => t.Id, t => t.Name);
        var courtLookup = courts.ToDictionary(c => c.Id, c => c.Name);

        var dtos = savedGames.Select(g =>
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

        return Results.Created($"/api/tournaments/{tournamentId}/games?phaseId={phaseId}", dtos);
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

        IEnumerable<Game> games;
        if (phaseId is not null)
            games = await gameRepository.GetByPhaseIdAsync(tournamentId, phaseId);
        else
            games = await gameRepository.GetByTournamentIdAsync(tournamentId);

        var gameList = games.ToList();

        // Apply additional filters
        if (groupId is not null)
            gameList = gameList.Where(g => g.GroupId == groupId).ToList();
        if (courtId is not null)
            gameList = gameList.Where(g => g.CourtId == courtId).ToList();

        // Resolve names
        var teams = (await teamRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var courts = (await courtRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var teamLookup = teams.ToDictionary(t => t.Id, t => t.Name);
        var courtLookup = courts.ToDictionary(c => c.Id, c => c.Name);

        var dtos = gameList
            .OrderBy(g => g.StartTime)
            .Select(g =>
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

        return Results.Ok(dtos);
    }

    private static async Task<IResult> DeleteGames(
        string tournamentId,
        string phaseId,
        ITournamentRepository tournamentRepository,
        IGameRepository gameRepository,
        ICurrentUserService currentUser)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        if (!tournament.IsOwnedBy(currentUser.UserId!))
            throw new ForbiddenException();

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
        return Results.Ok(mapper.Map<GameDto>(updatedGame));
    }
}
