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
        group.MapPost("/{phaseId}/auto-assign", AutoAssignTeams).RequireAuthorization();
        group.MapPost("/{phaseId}/complete", CompletePhase).RequireAuthorization();
        group.MapPost("/{phaseId}/reopen", ReopenPhase).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> AddPhase(
        string tournamentId,
        PhaseDto request,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        PhaseCompletionService phaseCompletionService,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var format = Enum.Parse<PhaseFormat>(request.Format, ignoreCase: true);
        var numberOfGroups = request.NumberOfGroups ?? 1;
        var startTime = mapper.Map<TimeOnly?>(request.StartTime);

        var phase = structure.AddPhase(request.Name, format, numberOfGroups,
            request.GroupWinners, request.TotalTeamsProceeding, startTime,
            request.NumberOfLevels);
        await structureRepository.UpdateAsync(structure);

        await phaseCompletionService.CreatePlaceholdersForNewPhaseAsync(structure, phase, tournamentId);

        var dto = mapper.Map<PhaseDto>(phase);
        return Results.Created($"/api/tournaments/{tournamentId}/structure/phases/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdatePhase(
        string tournamentId,
        string phaseId,
        PhaseDto request,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        PhaseCompletionService phaseCompletionService,
        PhaseGuardService phaseGuardService,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);
        phaseGuardService.EnsureEditable(phase);

        var format = Enum.Parse<PhaseFormat>(request.Format, ignoreCase: true);
        var startTime = mapper.Map<TimeOnly?>(request.StartTime);

        if (phase.HasStructuralChanges(format, startTime))
            await phaseGuardService.EnsureStructureEditableAsync(phase, tournamentId);

        var oldGroupWinners = phase.GroupWinners;
        var oldTotalTeamsProceeding = phase.TotalTeamsProceeding;
        structure.UpdatePhase(phaseId, request.Name, format,
            request.GroupWinners, request.TotalTeamsProceeding, startTime);
        await structureRepository.UpdateAsync(structure);

        await phaseCompletionService.RegeneratePlaceholdersOnUpdateAsync(
            structure, phaseId, tournamentId,
            oldGroupWinners, oldTotalTeamsProceeding,
            request.GroupWinners, request.TotalTeamsProceeding);

        var updated = structure.GetPhase(phaseId);
        var dto = mapper.Map<PhaseDto>(updated);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeletePhase(
        string tournamentId,
        string phaseId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        PhaseCompletionService phaseCompletionService,
        PhaseGuardService phaseGuardService,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);
        await phaseGuardService.EnsureDeletableAsync(phase, tournamentId);

        await phaseCompletionService.HandlePhaseDeletionAsync(structure, phaseId, tournamentId);

        return Results.NoContent();
    }

    private static async Task<IResult> CompletePhase(
        string tournamentId,
        string phaseId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        PhaseCompletionService phaseCompletionService,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = await phaseCompletionService.CompletePhaseAsync(tournamentId, phaseId, structure);

        var dto = mapper.Map<PhaseDto>(phase);
        return Results.Ok(dto);
    }

    private static async Task<IResult> ReopenPhase(
        string tournamentId,
        string phaseId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        PhaseCompletionService phaseCompletionService,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = await phaseCompletionService.ReopenPhaseAsync(tournamentId, phaseId, structure);

        var dto = mapper.Map<PhaseDto>(phase);
        return Results.Ok(dto);
    }

    private static async Task<IResult> AutoAssignTeams(
        string tournamentId,
        string phaseId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        ITeamRepository teamRepository,
        PhaseGuardService phaseGuardService,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);
        await phaseGuardService.EnsureStructureEditableAsync(phase, tournamentId);

        if (phase.Groups.Count == 0)
            throw new ValidationException(
                [new ValidationFailure("Groups", "Phase has no groups to assign teams to.")]);

        if (phase.Order > 1)
        {
            // Phase 2+: use placeholder teams ordered by seed
            var previousPhase = structure.GetPreviousPhase(phase.Id);
            if (previousPhase is null)
                throw new ValidationException(
                    [new ValidationFailure("Phase", "Previous phase not found.")]);

            var placeholderTeams = (await teamRepository.GetBySourcePhaseIdAsync(tournamentId, previousPhase.Id))
                .OrderBy(t => t.Seed)
                .ToList();

            if (placeholderTeams.Count == 0)
                throw new ValidationException(
                    [new ValidationFailure("Teams", "No placeholder teams found. Configure progression on the previous phase first.")]);

            var orderedIds = placeholderTeams.Select(t => t.EffectiveId).ToList();
            var sourceLevelCount = previousPhase.Levels.Count;
            structure.AutoAssignTeams(phaseId, orderedIds, sourceLevelCount);
        }
        else
        {
            // Phase 1: use real teams ordered by level
            var teams = (await teamRepository.GetByTournamentIdAsync(tournamentId))
                .Where(t => !t.IsPlaceholder)
                .ToList();
            structure.AutoAssignTeams(phaseId, teams);
        }

        await structureRepository.UpdateAsync(structure);

        var dto = mapper.Map<PhaseDto>(phase);
        return Results.Ok(dto);
    }
}
