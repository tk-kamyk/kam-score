using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Api.Helpers;

namespace KamSquare.KamScore.Api.Endpoints;

public static class LevelEndpoints
{
    public static RouteGroupBuilder MapLevelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}/structure/phases/{phaseId}/levels")
            .WithTags("Levels");

        group.MapPut("/{levelId}", UpdateLevel).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> UpdateLevel(
        string tournamentId,
        string phaseId,
        string levelId,
        LevelDto request,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        PhaseGuardService phaseGuardService,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);
        await phaseGuardService.EnsureEditableAsync(phase);

        if (structure.LevelNameExistsInPhase(phaseId, request.Name, levelId))
            throw new ValidationException(
                [new ValidationFailure("Name", "A level with that name already exists in this phase.")]);

        structure.UpdateLevel(phaseId, levelId, request.Name);
        await structureRepository.UpdateAsync(structure);

        var updated = structure.GetLevel(phaseId, levelId);
        var dto = mapper.Map<LevelDto>(updated);
        return Results.Ok(dto);
    }
}
