using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Api.Helpers;
using FluentValidation;
using FluentValidation.Results;

namespace KamSquare.KamScore.Api.Endpoints;

public static class GroupEndpoints
{
    public static RouteGroupBuilder MapGroupEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}/structure/phases/{phaseId}/groups")
            .WithTags("Groups");

        group.MapPost("/", AddGroup).RequireAuthorization();
        group.MapPut("/{groupId}", UpdateGroup).RequireAuthorization();
        group.MapDelete("/{groupId}", DeleteGroup).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> AddGroup(
        string tournamentId,
        string phaseId,
        GroupDto request,
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
        await phaseGuardService.EnsureStructureEditableAsync(phase, tournamentId);

        if (structure.GroupNameExistsInPhase(phaseId, request.Name))
            throw new ValidationException(
                [new ValidationFailure("Name", $"A group with name '{request.Name}' already exists in this phase.")]);

        var newGroup = structure.AddGroup(phaseId, request.Name);
        await structureRepository.UpdateAsync(structure);

        var dto = mapper.Map<GroupDto>(newGroup);
        return Results.Created(
            $"/api/tournaments/{tournamentId}/structure/phases/{phaseId}/groups/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateGroup(
        string tournamentId,
        string phaseId,
        string groupId,
        GroupDto request,
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

        if (structure.GroupNameExistsInPhase(phaseId, request.Name, groupId))
            throw new ValidationException(
                [new ValidationFailure("Name", $"A group with name '{request.Name}' already exists in this phase.")]);

        structure.UpdateGroup(phaseId, groupId, request.Name);
        await structureRepository.UpdateAsync(structure);

        var updated = structure.GetGroup(phaseId, groupId);
        var dto = mapper.Map<GroupDto>(updated);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteGroup(
        string tournamentId,
        string phaseId,
        string groupId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        PhaseGuardService phaseGuardService,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);
        await phaseGuardService.EnsureStructureEditableAsync(phase, tournamentId);

        structure.RemoveGroup(phaseId, groupId);
        await structureRepository.UpdateAsync(structure);

        return Results.NoContent();
    }
}
