using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Api.Helpers;
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
        ScheduleGenerationService scheduleGenerationService,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        var tournament = await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);
        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var savedGames = await scheduleGenerationService.GenerateAndScheduleAsync(
            tournament, tournamentId, phaseId, structure);

        var teams = (await teamRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var courts = (await courtRepository.GetByTournamentIdAsync(tournamentId)).ToList();

        return Results.Created(
            $"/api/tournaments/{tournamentId}/games?phaseId={phaseId}",
            EnrichGamesWithNames(savedGames, teams, courts, structure, mapper));
    }

    private static async Task<IResult> GetGames(
        string tournamentId,
        string? phaseId,
        string? groupId,
        string? courtId,
        string? teamId,
        ITournamentRepository tournamentRepository,
        IGameRepository gameRepository,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        ITournamentStructureRepository structureRepository,
        IMapper mapper)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        var games = (await gameRepository.GetGamesAsync(tournamentId, phaseId, groupId, courtId, teamId)).ToList();

        var teams = (await teamRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var courts = (await courtRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId);

        return Results.Ok(EnrichGamesWithNames(games.OrderBy(g => g.StartTime), teams, courts, structure, mapper));
    }

    private static async Task<IResult> DeleteGames(
        string tournamentId,
        string phaseId,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        IGameRepository gameRepository,
        PhaseGuardService phaseGuardService,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);
        await phaseGuardService.EnsureGamesDeletableAsync(phase);

        await gameRepository.DeleteByPhaseIdAsync(tournamentId, phaseId);

        // Reset phase status to New since games were the trigger for status change
        if (phase.Status is PhaseStatus.Scheduled or PhaseStatus.InProgress)
        {
            structure.ResetPhase(phaseId);
            await structureRepository.UpdateAsync(structure);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> RecordResult(
        string tournamentId,
        string gameId,
        GameResultDto resultDto,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        IGameRepository gameRepository,
        PhaseGuardService phaseGuardService,
        ICurrentUserService currentUser,
        HttpContext httpContext,
        IMapper mapper)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        TournamentAuthorizationHelper.ValidateParticipantAccess(tournament, currentUser, httpContext);

        var game = await gameRepository.GetByIdAsync(tournamentId, gameId);
        if (game is null)
            throw new NotFoundException(nameof(Game), gameId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(game.PhaseId);
        await phaseGuardService.EnsureResultsCanBeRecordedAsync(phase);

        if (game.HomeTeamId is null || game.AwayTeamId is null)
            throw new ValidationException(
                [new ValidationFailure("Teams", "Cannot record result: both teams must be assigned.")]);

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

    private static List<GameDto> EnrichGamesWithNames(
        IEnumerable<Game> games, List<Team> teams, List<Court> courts,
        TournamentStructure? structure, IMapper mapper)
    {
        var teamLookup = teams.ToDictionary(t => t.Id, t => t.Name);
        var placeholderLookup = teams.Where(t => t.IsPlaceholder).Select(t => t.Id).ToHashSet();
        var courtLookup = courts.ToDictionary(c => c.Id, c => c.Name);

        var phaseLookup = structure?.Phases.ToDictionary(p => p.Id, p => p.Name) ?? [];
        var groupLookup = structure?.Phases
            .SelectMany(p => p.Groups)
            .ToDictionary(g => g.Id, g => g.Name) ?? [];
        var groupToLevelLookup = structure?.Phases
            .SelectMany(p => p.Groups.Where(g => g.LevelId is not null)
                .Select(g => new { g.Id, g.LevelId }))
            .ToDictionary(x => x.Id, x => x.LevelId!) ?? [];
        var levelLookup = structure?.Phases
            .SelectMany(p => p.Levels)
            .ToDictionary(l => l.Id, l => l.Name) ?? [];

        return games.Select(g =>
        {
            var dto = mapper.Map<GameDto>(g);

            string? levelName = null;
            if (groupToLevelLookup.TryGetValue(g.GroupId, out var levelId))
                levelLookup.TryGetValue(levelId, out levelName);

            return dto with
            {
                HomeTeamName = g.HomeTeamId is not null && teamLookup.TryGetValue(g.HomeTeamId, out var hn) ? hn : null,
                AwayTeamName = g.AwayTeamId is not null && teamLookup.TryGetValue(g.AwayTeamId, out var an) ? an : null,
                RefereeTeamName = g.RefereeTeamId is not null && teamLookup.TryGetValue(g.RefereeTeamId, out var rn) ? rn : null,
                CourtName = g.CourtId is not null && courtLookup.TryGetValue(g.CourtId, out var cn) ? cn : null,
                HomeTeamIsPlaceholder = g.HomeTeamId is not null && placeholderLookup.Contains(g.HomeTeamId) ? true : null,
                AwayTeamIsPlaceholder = g.AwayTeamId is not null && placeholderLookup.Contains(g.AwayTeamId) ? true : null,
                PhaseName = phaseLookup.TryGetValue(g.PhaseId, out var pn) ? pn : null,
                GroupName = groupLookup.TryGetValue(g.GroupId, out var gn) ? gn : null,
                LevelName = levelName
            };
        }).ToList();
    }
}
