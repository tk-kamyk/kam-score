using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;

namespace KamSquare.KamScore.Api.Endpoints;

public static class StructureEndpoints
{
    public static RouteGroupBuilder MapStructureEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}/structure")
            .WithTags("Structure");

        group.MapGet("/", GetStructure);
        group.MapPost("/", InitializeStructure).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetStructure(
        string tournamentId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        IMapper mapper)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId);
        if (structure is null)
            return Results.Ok(new TournamentStructureDto(null, tournamentId, []));

        var dto = mapper.Map<TournamentStructureDto>(structure);
        return Results.Ok(dto);
    }

    private static async Task<IResult> InitializeStructure(
        string tournamentId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        if (!tournament.IsOwnedBy(currentUser.UserId!))
            throw new ForbiddenException();

        var existing = await structureRepository.GetByTournamentIdAsync(tournamentId);
        if (existing is not null)
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("Structure", "Tournament structure has already been initialized.")]);

        var structure = TournamentStructure.Create(tournamentId);
        var created = await structureRepository.CreateAsync(structure);
        var dto = mapper.Map<TournamentStructureDto>(created);

        return Results.Created($"/api/tournaments/{tournamentId}/structure", dto);
    }
}
