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
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

        var format = Enum.Parse<PhaseFormat>(request.Format, ignoreCase: true);
        var startTime = mapper.Map<TimeOnly?>(request.StartTime);
        structure.UpdatePhase(phaseId, request.Name, format,
            request.GroupWinners, request.TotalTeamsProceeding, startTime);
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
        await tournamentRepository.GetOwnedTournamentAsync(currentUser, tournamentId);

        var structure = await structureRepository.GetByTournamentIdAsync(tournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), tournamentId);

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

            // Assign teams to next phase groups
            structure.AutoAssignTeams(nextPhase.Id, seededIds);
            structure.ActivatePhase(nextPhase.Id);

            // Resolve cross-phase placeholders in next phase games (if any exist)
            var nextPhaseGames = (await gameRepository.GetByPhaseIdAsync(tournamentId, nextPhase.Id)).ToList();
            if (nextPhaseGames.Count > 0)
            {
                var seedMapping = CrossPhasePlaceholderResolver.BuildSeedMapping(seededIds);
                var modifiedGames = CrossPhasePlaceholderResolver.Resolve(nextPhaseGames, seedMapping, phase.Name);
                foreach (var game in modifiedGames)
                {
                    await gameRepository.UpdateAsync(game);
                }
            }
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

        structure.ReopenPhase(phaseId);

        // Unresolve cross-phase placeholders in next phase games
        if (nextPhase is not null)
        {
            var nextPhaseGames = (await gameRepository.GetByPhaseIdAsync(tournamentId, nextPhase.Id)).ToList();
            if (nextPhaseGames.Count > 0)
            {
                var modifiedGames = CrossPhasePlaceholderResolver.Unresolve(nextPhaseGames, phase.Name);
                foreach (var game in modifiedGames)
                {
                    await gameRepository.UpdateAsync(game);
                }
            }
        }

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

        var teams = (await teamRepository.GetByTournamentIdAsync(tournamentId)).ToList();
        structure.AutoAssignTeams(phaseId, teams);
        await structureRepository.UpdateAsync(structure);

        var dto = mapper.Map<PhaseDto>(phase);
        return Results.Ok(dto);
    }
}
