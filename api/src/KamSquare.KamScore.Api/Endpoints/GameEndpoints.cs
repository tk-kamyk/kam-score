using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
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

        var games = (await gameRepository.GetGamesAsync(tournamentId, phaseId, groupId, courtId)).ToList();

        var teams = (await teamRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        var courts = (await courtRepository.GetByTournamentIdAsync(tournamentId)).ToList();

        return Results.Ok(EnrichGamesWithNames(games.OrderBy(g => g.StartTime), teams, courts, mapper));
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

        TournamentAuthorizationHelper.ValidateParticipantAccess(tournament, currentUser, httpContext);

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

    private static List<GameDto> EnrichGamesWithNames(
        IEnumerable<Game> games, List<Team> teams, List<Court> courts, IMapper mapper)
    {
        var teamLookup = teams.ToDictionary(t => t.Id, t => t.Name);
        var placeholderLookup = teams.Where(t => t.IsPlaceholder).Select(t => t.Id).ToHashSet();
        var courtLookup = courts.ToDictionary(c => c.Id, c => c.Name);

        return games.Select(g =>
        {
            var dto = mapper.Map<GameDto>(g);
            return dto with
            {
                HomeTeamName = g.HomeTeamId is not null && teamLookup.TryGetValue(g.HomeTeamId, out var hn) ? hn : null,
                AwayTeamName = g.AwayTeamId is not null && teamLookup.TryGetValue(g.AwayTeamId, out var an) ? an : null,
                RefereeTeamName = g.RefereeTeamId is not null && teamLookup.TryGetValue(g.RefereeTeamId, out var rn) ? rn : null,
                CourtName = g.CourtId is not null && courtLookup.TryGetValue(g.CourtId, out var cn) ? cn : null,
                HomeTeamIsPlaceholder = g.HomeTeamId is not null && placeholderLookup.Contains(g.HomeTeamId) ? true : null,
                AwayTeamIsPlaceholder = g.AwayTeamId is not null && placeholderLookup.Contains(g.AwayTeamId) ? true : null
            };
        }).ToList();
    }
}
