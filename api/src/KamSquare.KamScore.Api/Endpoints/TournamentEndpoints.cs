using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Api.Endpoints;

public static class TournamentEndpoints
{
    public static RouteGroupBuilder MapTournamentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments")
            .WithTags("Tournaments");

        group.MapGet("/", GetTournaments);
        group.MapGet("/{id}", GetTournament);
        group.MapPost("/", CreateTournament).RequireAuthorization();
        group.MapPut("/{id}", UpdateTournament).RequireAuthorization();
        group.MapDelete("/{id}", DeleteTournament).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetTournaments(
        ITournamentRepository repository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        if (currentUser.IsAuthenticated)
        {
            var tournaments = await repository.GetByOwnerIdAsync(currentUser.UserId!);
            var dtos = mapper.Map<IEnumerable<TournamentDto>>(tournaments);
            return Results.Ok(dtos);
        }

        var allTournaments = await repository.GetAllAsync();
        var allDtos = mapper.Map<IEnumerable<TournamentDto>>(allTournaments)
            .Select(HideTournamentCode);
        return Results.Ok(allDtos);
    }

    private static async Task<IResult> GetTournament(
        string id,
        ITournamentRepository repository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        var tournament = await repository.GetByIdAsync(id);

        if (tournament is null)
        {
            throw new NotFoundException(nameof(Tournament), id);
        }

        var dto = mapper.Map<TournamentDto>(tournament);

        if (!currentUser.IsAuthenticated || !tournament.IsOwnedBy(currentUser.UserId!))
        {
            dto = HideTournamentCode(dto);
        }

        return Results.Ok(dto);
    }

    private static async Task<IResult> CreateTournament(
        TournamentDto request,
        ITournamentRepository repository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        var discipline = Enum.Parse<Discipline>(request.Discipline, ignoreCase: true);
        var tournament = Tournament.Create(request.Name, discipline, currentUser.UserId!);

        if (request.StartTime.HasValue || request.GameLength.HasValue || request.GameConditions is not null)
        {
            var gameConditions = request.GameConditions is not null
                ? mapper.Map<GameConditions>(request.GameConditions)
                : null;
            tournament.Update(request.Name, discipline, request.StartTime, request.GameLength, gameConditions);
        }

        var created = await repository.CreateAsync(tournament);
        var dto = mapper.Map<TournamentDto>(created);

        return Results.Created($"/api/tournaments/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateTournament(
        string id,
        TournamentDto request,
        ITournamentRepository repository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        var tournament = await repository.GetByIdAsync(id);

        if (tournament is null)
        {
            throw new NotFoundException(nameof(Tournament), id);
        }

        if (!tournament.IsOwnedBy(currentUser.UserId!))
        {
            throw new ForbiddenException();
        }

        var discipline = Enum.Parse<Discipline>(request.Discipline, ignoreCase: true);
        var gameConditions = request.GameConditions is not null
            ? mapper.Map<GameConditions>(request.GameConditions)
            : null;

        tournament.Update(request.Name, discipline, request.StartTime, request.GameLength, gameConditions);

        var updated = await repository.UpdateAsync(tournament);
        var dto = mapper.Map<TournamentDto>(updated);

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteTournament(
        string id,
        ITournamentRepository repository,
        ICurrentUserService currentUser)
    {
        var tournament = await repository.GetByIdAsync(id);

        if (tournament is null)
        {
            throw new NotFoundException(nameof(Tournament), id);
        }

        if (!tournament.IsOwnedBy(currentUser.UserId!))
        {
            throw new ForbiddenException();
        }

        await repository.DeleteAsync(id, tournament.OwnerId);

        return Results.NoContent();
    }

    private static TournamentDto HideTournamentCode(TournamentDto dto)
    {
        return dto with { TournamentCode = null };
    }
}
