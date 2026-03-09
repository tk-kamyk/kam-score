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

}
