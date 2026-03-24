using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Api.Helpers;
using FluentValidation;
using FluentValidation.Results;

namespace KamSquare.KamScore.Api.Endpoints;

public static class CourtEndpoints
{
    public static RouteGroupBuilder MapCourtEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}/courts")
            .WithTags("Courts");

        group.MapGet("/", GetCourts);
        group.MapPost("/", CreateCourt).RequireAuthorization();
        group.MapPost("/generate", GenerateCourts).RequireAuthorization();
        group.MapPut("/{courtId}", UpdateCourt).RequireAuthorization();
        group.MapDelete("/{courtId}", DeleteCourt).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetCourts(
        string tournamentId,
        ICourtRepository courtRepository,
        ITournamentRepository tournamentRepository,
        IMapper mapper)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        var courts = await courtRepository.GetByTournamentIdAsync(tournamentId);
        var dtos = mapper.Map<IEnumerable<CourtDto>>(courts);

        return Results.Ok(dtos);
    }

    private static async Task<IResult> CreateCourt(
        string tournamentId,
        CourtDto request,
        ICourtRepository courtRepository,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        if (await courtRepository.ExistsByNameAsync(tournamentId, request.Name))
            throw new ValidationException(
                [new ValidationFailure("Name", $"A court with name '{request.Name}' already exists in this tournament.")]);

        var court = Court.Create(request.Name, tournamentId);
        var created = await courtRepository.CreateAsync(court);
        var dto = mapper.Map<CourtDto>(created);

        return Results.Created($"/api/tournaments/{tournamentId}/courts/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateCourt(
        string tournamentId,
        string courtId,
        CourtDto request,
        ICourtRepository courtRepository,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var court = await courtRepository.GetByIdAsync(courtId, tournamentId);
        if (court is null)
            throw new NotFoundException(nameof(Court), courtId);

        if (await courtRepository.ExistsByNameAsync(tournamentId, request.Name, courtId))
            throw new ValidationException(
                [new ValidationFailure("Name", $"A court with name '{request.Name}' already exists in this tournament.")]);

        court.Update(request.Name);
        var updated = await courtRepository.UpdateAsync(court);
        var dto = mapper.Map<CourtDto>(updated);

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteCourt(
        string tournamentId,
        string courtId,
        ICourtRepository courtRepository,
        ITournamentRepository tournamentRepository,
        IGameRepository gameRepository,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var court = await courtRepository.GetByIdAsync(courtId, tournamentId);
        if (court is null)
            throw new NotFoundException(nameof(Court), courtId);

        var gamesOnCourt = await gameRepository.GetGamesAsync(tournamentId, courtId: courtId);
        if (gamesOnCourt.Any())
            throw new ReferentialIntegrityException("court", court.Name,
                "court has scheduled games. Delete those games first");

        await courtRepository.DeleteAsync(courtId, tournamentId);

        return Results.NoContent();
    }

    private static async Task<IResult> GenerateCourts(
        string tournamentId,
        GenerateCourtsDto request,
        ICourtRepository courtRepository,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var existingCount = await courtRepository.CountByTournamentIdAsync(tournamentId);
        var courts = Court.GenerateCourts(request.Count, existingCount + 1, tournamentId);
        var created = await courtRepository.CreateBatchAsync(courts);
        var dtos = mapper.Map<IEnumerable<CourtDto>>(created);

        return Results.Created($"/api/tournaments/{tournamentId}/courts", dtos);
    }
}
