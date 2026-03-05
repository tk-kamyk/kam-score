using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services;
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
        ITeamRepository teamRepository,
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
            request.GroupWinners, request.TotalTeamsProceeding, startTime);
        await structureRepository.UpdateAsync(structure);

        // Auto-create placeholder teams if previous phase has progression config
        if (phase.Order > 1)
        {
            var previousPhase = structure.GetPreviousPhase(phase.Id);
            if (previousPhase is not null)
            {
                var placeholders = PlaceholderTeamGenerator.Generate(previousPhase, tournamentId);
                if (placeholders is not null)
                {
                    await teamRepository.CreateBatchAsync(placeholders);
                }
            }
        }

        var dto = mapper.Map<PhaseDto>(phase);
        return Results.Created($"/api/tournaments/{tournamentId}/structure/phases/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdatePhase(
        string tournamentId,
        string phaseId,
        PhaseDto request,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        ITeamRepository teamRepository,
        IGameRepository gameRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);
        var oldGroupWinners = phase.GroupWinners;
        var oldTotalTeamsProceeding = phase.TotalTeamsProceeding;

        var format = Enum.Parse<PhaseFormat>(request.Format, ignoreCase: true);
        var startTime = mapper.Map<TimeOnly?>(request.StartTime);
        structure.UpdatePhase(phaseId, request.Name, format,
            request.GroupWinners, request.TotalTeamsProceeding, startTime);
        await structureRepository.UpdateAsync(structure);

        // Regenerate placeholder teams for the next phase if progression config changed
        var progressionChanged = oldGroupWinners != request.GroupWinners
                                 || oldTotalTeamsProceeding != request.TotalTeamsProceeding;
        if (progressionChanged)
        {
            var nextPhase = structure.GetNextPhase(phaseId);
            if (nextPhase is not null)
            {
                // Delete existing placeholder teams and games for the next phase
                await teamRepository.DeleteBySourcePhaseIdAsync(tournamentId, phaseId);
                await gameRepository.DeleteByPhaseIdAsync(tournamentId, nextPhase.Id);

                // Clear group assignments in next phase (placeholder team IDs are now invalid)
                foreach (var group in nextPhase.Groups)
                {
                    group.ClearTeams();
                }

                await structureRepository.UpdateAsync(structure);

                // Create new placeholder teams
                var updatedPhase = structure.GetPhase(phaseId);
                var placeholders = PlaceholderTeamGenerator.Generate(updatedPhase, tournamentId);
                if (placeholders is not null)
                {
                    await teamRepository.CreateBatchAsync(placeholders);
                }
            }
        }

        var updated = structure.GetPhase(phaseId);
        var dto = mapper.Map<PhaseDto>(updated);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeletePhase(
        string tournamentId,
        string phaseId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        ITeamRepository teamRepository,
        ICurrentUserService currentUser)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        // Delete placeholder teams that were created from this phase's progression
        await teamRepository.DeleteBySourcePhaseIdAsync(tournamentId, phaseId);

        structure.RemovePhase(phaseId);
        await structureRepository.UpdateAsync(structure);

        return Results.NoContent();
    }

    private static async Task<IResult> CompletePhase(
        string tournamentId,
        string phaseId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        IGameRepository gameRepository,
        ITeamRepository teamRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);

        if (phase.Status != PhaseStatus.InProgress)
            throw new ValidationException(
                [new ValidationFailure("Status", "Phase must be in progress to complete.")]);

        var phaseGames = (await gameRepository.GetByPhaseIdAsync(tournamentId, phaseId)).ToList();
        if (phaseGames.Count == 0 || phaseGames.Any(g => g.Status != GameStatus.Completed))
            throw new ValidationException(
                [new ValidationFailure("Games", "All games must be completed before completing the phase.")]);

        structure.CompletePhase(phaseId);

        var nextPhase = structure.GetNextPhase(phaseId);
        var hasProgressionConfig = phase.GroupWinners is not null || phase.TotalTeamsProceeding is not null;

        if (nextPhase is not null && hasProgressionConfig)
        {
            // Calculate standings for each group
            var groupStandings = phase.Groups
                .Select(g =>
                {
                    var groupGames = phaseGames.Where(game => game.GroupId == g.Id).ToList();
                    var standings = StandingsCalculator.Calculate(phase.Format, groupGames, g.TeamIds);
                    return (g.Id, standings);
                })
                .ToList();

            // Calculate qualifying teams and seeding
            var qualifyingIds = PhaseAdvancementCalculator.CalculateQualifyingTeamIds(phase, groupStandings);
            var seededIds = PhaseAdvancementCalculator.CalculateSeeding(qualifyingIds, groupStandings);

            // Resolve placeholder teams to real teams
            var placeholderTeams = (await teamRepository.GetBySourcePhaseIdAsync(tournamentId, phaseId)).ToList();
            if (placeholderTeams.Count > 0)
            {
                var nextPhaseGames = (await gameRepository.GetByPhaseIdAsync(tournamentId, nextPhase.Id)).ToList();
                var modifiedGames = PlaceholderResolver.Resolve(nextPhaseGames, nextPhase, placeholderTeams, seededIds);

                foreach (var game in modifiedGames)
                {
                    await gameRepository.UpdateAsync(game);
                }

                foreach (var placeholder in placeholderTeams)
                {
                    await teamRepository.UpdateAsync(placeholder);
                }
            }
            else
            {
                // No placeholder teams — assign real teams directly (legacy/fallback)
                structure.AutoAssignTeams(nextPhase.Id, seededIds);
            }

            structure.ActivatePhase(nextPhase.Id);
        }

        await structureRepository.UpdateAsync(structure);

        var dto = mapper.Map<PhaseDto>(phase);
        return Results.Ok(dto);
    }

    private static async Task<IResult> ReopenPhase(
        string tournamentId,
        string phaseId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        IGameRepository gameRepository,
        ITeamRepository teamRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);

        if (phase.Status != PhaseStatus.Completed)
            throw new ValidationException(
                [new ValidationFailure("Status", "Phase must be completed to reopen.")]);

        var nextPhase = structure.GetNextPhase(phaseId);

        // Unresolve placeholder teams before reopening
        if (nextPhase is not null)
        {
            var placeholderTeams = (await teamRepository.GetBySourcePhaseIdAsync(tournamentId, phaseId)).ToList();
            if (placeholderTeams.Count > 0)
            {
                var nextPhaseGames = (await gameRepository.GetByPhaseIdAsync(tournamentId, nextPhase.Id)).ToList();
                var modifiedGames = PlaceholderResolver.Unresolve(nextPhaseGames, nextPhase, placeholderTeams);

                foreach (var game in modifiedGames)
                {
                    await gameRepository.UpdateAsync(game);
                }

                foreach (var placeholder in placeholderTeams)
                {
                    await teamRepository.UpdateAsync(placeholder);
                }
            }
        }

        structure.ReopenPhase(phaseId);
        await structureRepository.UpdateAsync(structure);

        var dto = mapper.Map<PhaseDto>(phase);
        return Results.Ok(dto);
    }

    private static async Task<IResult> AutoAssignTeams(
        string tournamentId,
        string phaseId,
        ITournamentStructureRepository structureRepository,
        ITournamentRepository tournamentRepository,
        ITeamRepository teamRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var phase = structure.GetPhase(phaseId);

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

            var orderedIds = placeholderTeams.Select(t => t.Id).ToList();
            structure.AutoAssignTeams(phaseId, orderedIds);
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
