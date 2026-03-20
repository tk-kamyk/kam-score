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
        group.MapPut("/games/{gameId}/referee", AssignReferee)
            .RequireAuthorization();
        group.MapGet("/games/{gameId}/referee-candidates", GetRefereeCandidates)
            .RequireAuthorization();
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
            GameEnrichmentHelper.EnrichGamesWithNames(savedGames, teams, courts, structure, mapper));
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

        return Results.Ok(GameEnrichmentHelper.EnrichGamesWithNames(games.OrderBy(g => g.StartTime), teams, courts, structure, mapper));
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
        phaseGuardService.EnsureGamesDeletable(phase);

        await gameRepository.DeleteByPhaseIdAsync(tournamentId, phaseId);

        // Reset phase status to New since games were the trigger for status change
        if (phase.Status is PhaseStatus.Scheduled or PhaseStatus.InProgress)
        {
            structure.ResetPhase(phaseId);
            await structureRepository.UpdateAsync(structure);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> AssignReferee(
        string tournamentId,
        string gameId,
        AssignRefereeDto dto,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        IGameRepository gameRepository,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        throw new NotImplementedException();
    }

    private static async Task<IResult> GetRefereeCandidates(
        string tournamentId,
        string gameId,
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        IGameRepository gameRepository,
        ITeamRepository teamRepository,
        ICurrentUserService currentUser)
    {
        throw new NotImplementedException();
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
        phaseGuardService.EnsureResultsCanBeRecorded(phase);

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
            await Task.WhenAll(advancedGames.Select(g => gameRepository.UpdateAsync(g)));
        }

        return Results.Ok(mapper.Map<GameDto>(updatedGame));
    }

}
