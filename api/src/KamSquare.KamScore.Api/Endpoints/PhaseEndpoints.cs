using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;

namespace KamSquare.KamScore.Api.Endpoints;

public static class PhaseEndpoints
{
    public static RouteGroupBuilder MapPhaseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}/structure/phases")
            .WithTags("Phases");

        group.MapPost("/", AddPhase).RequireAuthorization();
        group.MapPut("/{phaseId}", UpdatePhase).RequireAuthorization();
        group.MapDelete("/{phaseId}", DeletePhase).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> AddPhase(
        string tournamentId,
        PhaseDto request,
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

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var format = Enum.Parse<PhaseFormat>(request.Format, ignoreCase: true);
        var numberOfGroups = request.NumberOfGroups ?? 1;

        var phase = structure.AddPhase(request.Name, format, numberOfGroups);
        await structureRepository.UpdateAsync(structure);

        var dto = mapper.Map<PhaseDto>(phase);
        return Results.Created($"/api/tournaments/{tournamentId}/structure/phases/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdatePhase(
        string tournamentId,
        string phaseId,
        PhaseDto request,
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

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var format = Enum.Parse<PhaseFormat>(request.Format, ignoreCase: true);
        structure.UpdatePhase(phaseId, request.Name, format);
        await structureRepository.UpdateAsync(structure);

        var updated = structure.GetPhase(phaseId);
        var dto = mapper.Map<PhaseDto>(updated);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeletePhase(
        string tournamentId,
        string phaseId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        ICurrentUserService currentUser)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);

        if (!tournament.IsOwnedBy(currentUser.UserId!))
            throw new ForbiddenException();

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        structure.RemovePhase(phaseId);
        await structureRepository.UpdateAsync(structure);

        return Results.NoContent();
    }
}
