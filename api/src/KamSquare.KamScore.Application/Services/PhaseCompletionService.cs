using FluentValidation;
using FluentValidation.Results;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;

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
                structure.AutoAssignTeams(nextPhase.Id, seededIds);
            }

            structure.ActivatePhase(nextPhase.Id);
        }

        await _structureRepository.UpdateAsync(structure);

        return phase;
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

        // Unresolve placeholder teams before reopening
        if (nextPhase is not null)
        {
            var placeholderTeams = (await _teamRepository.GetBySourcePhaseIdAsync(tournamentId, phaseId)).ToList();
            if (placeholderTeams.Count > 0)
            {
                var nextPhaseGames = (await _gameRepository.GetByPhaseIdAsync(tournamentId, nextPhase.Id)).ToList();
                var modifiedGames = PlaceholderResolver.Unresolve(nextPhaseGames, nextPhase, placeholderTeams);

                await Task.WhenAll(modifiedGames.Select(game => _gameRepository.UpdateAsync(game)));
                await Task.WhenAll(placeholderTeams.Select(placeholder => _teamRepository.UpdateAsync(placeholder)));
            }
        }

        structure.ReopenPhase(phaseId);
        await _structureRepository.UpdateAsync(structure);

        return phase;
    }
}
