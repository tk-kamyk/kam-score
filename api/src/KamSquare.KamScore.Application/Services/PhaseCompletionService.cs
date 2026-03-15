using FluentValidation;
using FluentValidation.Results;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Application.Services;

public class PhaseCompletionService
{
    private readonly IGameRepository _gameRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ITournamentStructureRepository _structureRepository;

    public PhaseCompletionService(
        IGameRepository gameRepository,
        ITeamRepository teamRepository,
        ITournamentStructureRepository structureRepository)
    {
        _gameRepository = gameRepository;
        _teamRepository = teamRepository;
        _structureRepository = structureRepository;
    }

    public async Task<Phase> CompletePhaseAsync(
        string tournamentId,
        string phaseId,
        TournamentStructure structure)
    {
        var phase = structure.GetPhase(phaseId);

        if (phase.Status != PhaseStatus.InProgress)
            throw new ValidationException(
                [new ValidationFailure("Status", "Phase must be in progress to complete.")]);

        var phaseGames = (await _gameRepository.GetByPhaseIdAsync(tournamentId, phaseId)).ToList();
        if (phaseGames.Count == 0 || phaseGames.Any(g => g.Status != GameStatus.Completed))
            throw new ValidationException(
                [new ValidationFailure("Games", "All games must be completed before completing the phase.")]);

        structure.CompletePhase(phaseId);

        var nextPhase = structure.GetNextPhase(phaseId);
        var hasProgressionConfig = phase.HasProgressionConfig;

        if (nextPhase is not null && hasProgressionConfig)
        {
            var groupStandings = CalculateGroupStandings(phase, phaseGames);

            // Calculate qualifying teams and seeding
            var qualifyingIds = PhaseAdvancementCalculator.CalculateQualifyingTeamIds(phase, groupStandings);
            var seededIds = PhaseAdvancementCalculator.CalculateSeeding(qualifyingIds, groupStandings, phase);

            // Resolve placeholder teams to real teams
            var placeholderTeams = (await _teamRepository.GetBySourcePhaseIdAsync(tournamentId, phaseId)).ToList();
            if (placeholderTeams.Count > 0)
            {
                var nextPhaseGames = (await _gameRepository.GetByPhaseIdAsync(tournamentId, nextPhase.Id)).ToList();
                var modifiedGames = PlaceholderResolver.Resolve(nextPhaseGames, nextPhase, placeholderTeams, seededIds);

                await Task.WhenAll(modifiedGames.Select(game => _gameRepository.UpdateAsync(game)));
                await Task.WhenAll(placeholderTeams.Select(placeholder => _teamRepository.UpdateAsync(placeholder)));
            }
            else
            {
                // No placeholder teams -- assign real teams directly (legacy/fallback)
                structure.AutoAssignTeams(nextPhase.Id, seededIds, phase.Levels.Count);
            }

            // Activate next phase now that teams are resolved (Scheduled -> InProgress)
            if (nextPhase.Status == PhaseStatus.Scheduled)
            {
                structure.ActivatePhase(nextPhase.Id);
            }
        }

        await _structureRepository.UpdateAsync(structure);

        return phase;
    }

    public async Task CreatePlaceholdersForNewPhaseAsync(
        TournamentStructure structure,
        Phase phase,
        string tournamentId)
    {
        if (phase.Order <= 1)
            return;

        var previousPhase = structure.GetPreviousPhase(phase.Id);
        if (previousPhase is null)
            return;

        var placeholders = PlaceholderTeamGenerator.Generate(previousPhase, tournamentId);
        if (placeholders is null)
            return;

        await _teamRepository.CreateBatchAsync(placeholders);

        // If previous phase is already completed, resolve placeholders immediately
        if (previousPhase.Status == PhaseStatus.Completed)
        {
            await ResolveNewPlaceholdersAsync(tournamentId, previousPhase, placeholders);
        }
    }

    private async Task ResolveNewPlaceholdersAsync(
        string tournamentId,
        Phase completedPhase,
        List<Team> placeholders)
    {
        var phaseGames = (await _gameRepository.GetByPhaseIdAsync(tournamentId, completedPhase.Id)).ToList();
        if (phaseGames.Count == 0)
            return;

        var groupStandings = CalculateGroupStandings(completedPhase, phaseGames);

        var qualifyingIds = PhaseAdvancementCalculator.CalculateQualifyingTeamIds(completedPhase, groupStandings);
        var seededIds = PhaseAdvancementCalculator.CalculateSeeding(qualifyingIds, groupStandings, completedPhase);

        var ordered = placeholders.OrderBy(t => t.Seed).ToList();
        for (var i = 0; i < ordered.Count && i < seededIds.Count; i++)
        {
            ordered[i].ResolvedTeamId = seededIds[i];
            ordered[i].LastModified = DateTime.UtcNow;
        }

        await Task.WhenAll(placeholders.Select(p => _teamRepository.UpdateAsync(p)));
    }

    public async Task RegeneratePlaceholdersOnUpdateAsync(
        TournamentStructure structure,
        string phaseId,
        string tournamentId,
        int? oldGroupWinners,
        int? oldTotalTeamsProceeding,
        int? newGroupWinners,
        int? newTotalTeamsProceeding)
    {
        var progressionChanged = oldGroupWinners != newGroupWinners
                                 || oldTotalTeamsProceeding != newTotalTeamsProceeding;
        if (!progressionChanged)
            return;

        var nextPhase = structure.GetNextPhase(phaseId);
        if (nextPhase is null)
            return;

        await _teamRepository.DeleteBySourcePhaseIdAsync(tournamentId, phaseId);
        await _gameRepository.DeleteByPhaseIdAsync(tournamentId, nextPhase.Id);

        foreach (var group in nextPhase.Groups)
        {
            group.ClearTeams();
        }

        await _structureRepository.UpdateAsync(structure);

        var updatedPhase = structure.GetPhase(phaseId);
        var placeholders = PlaceholderTeamGenerator.Generate(updatedPhase, tournamentId);
        if (placeholders is not null)
        {
            await _teamRepository.CreateBatchAsync(placeholders);
        }
    }

    private static List<(string GroupId, List<Standing> Standings)> CalculateGroupStandings(
        Phase phase, List<Game> phaseGames) =>
        phase.Groups
            .Select(g =>
            {
                var groupGames = phaseGames.Where(game => game.GroupId == g.Id).ToList();
                var standings = StandingsCalculator.Calculate(phase.Format, groupGames, g.TeamIds);
                return (g.Id, standings);
            })
            .ToList();

    public async Task HandlePhaseDeletionAsync(
        TournamentStructure structure,
        string phaseId,
        string tournamentId)
    {
        var previousPhase = structure.GetPreviousPhase(phaseId);
        var nextPhase = structure.GetNextPhase(phaseId);

        // Delete placeholder teams created FOR this phase (from previous phase's progression)
        if (previousPhase is not null)
        {
            await _teamRepository.DeleteBySourcePhaseIdAsync(tournamentId, previousPhase.Id);
        }

        // Delete placeholder teams created FROM this phase's progression (for next phase)
        await _teamRepository.DeleteBySourcePhaseIdAsync(tournamentId, phaseId);

        structure.RemovePhase(phaseId);

        // Regenerate placeholders for successor phase based on new adjacency
        await RegeneratePlaceholdersAfterDeletionAsync(
            structure, previousPhase, nextPhase, tournamentId);

        await _structureRepository.UpdateAsync(structure);
    }

    public async Task RegeneratePlaceholdersAfterDeletionAsync(
        TournamentStructure structure,
        Phase? newPreviousPhase,
        Phase? nextPhase,
        string tournamentId)
    {
        if (nextPhase is null)
            return;

        foreach (var group in nextPhase.Groups)
        {
            group.ClearTeams();
        }

        await _gameRepository.DeleteByPhaseIdAsync(tournamentId, nextPhase.Id);

        if (newPreviousPhase is not null)
        {
            var placeholders = PlaceholderTeamGenerator.Generate(newPreviousPhase, tournamentId);
            if (placeholders is not null)
            {
                await _teamRepository.CreateBatchAsync(placeholders);
            }
        }
    }

    public async Task<Phase> ReopenPhaseAsync(
        string tournamentId,
        string phaseId,
        TournamentStructure structure)
    {
        var phase = structure.GetPhase(phaseId);

        if (phase.Status != PhaseStatus.Completed)
            throw new ValidationException(
                [new ValidationFailure("Status", "Phase must be completed to reopen.")]);

        var nextPhase = structure.GetNextPhase(phaseId);

        if (nextPhase is not null)
        {
            var nextPhaseGames = (await _gameRepository.GetByPhaseIdAsync(tournamentId, nextPhase.Id)).ToList();

            // Block reopen if next phase has completed games
            if (nextPhaseGames.Any(g => g.Status == GameStatus.Completed))
                throw new PhaseStateException(phase.Name, "reopen",
                    "the next phase has completed games. Delete those results first");

            // Unresolve placeholder teams before reopening, or clear groups if no placeholders
            var placeholderTeams = (await _teamRepository.GetBySourcePhaseIdAsync(tournamentId, phaseId)).ToList();
            if (placeholderTeams.Count > 0)
            {
                var modifiedGames = PlaceholderResolver.Unresolve(nextPhaseGames, nextPhase, placeholderTeams);

                await Task.WhenAll(modifiedGames.Select(game => _gameRepository.UpdateAsync(game)));
                await Task.WhenAll(placeholderTeams.Select(placeholder => _teamRepository.UpdateAsync(placeholder)));
            }
            else
            {
                foreach (var group in nextPhase.Groups)
                    group.ClearTeams();
            }
        }

        structure.ReopenPhase(phaseId);
        await _structureRepository.UpdateAsync(structure);

        return phase;
    }
}
