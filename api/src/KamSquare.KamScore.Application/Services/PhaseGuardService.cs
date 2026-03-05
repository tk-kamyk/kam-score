using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;

namespace KamSquare.KamScore.Application.Services;

public class PhaseGuardService
{
    private readonly IGameRepository _gameRepository;

    public PhaseGuardService(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public Task EnsureEditableAsync(Phase phase)
    {
        if (phase.Status == PhaseStatus.Completed)
            throw new PhaseStateException(phase.Name, "edit", "phase is completed");

        return Task.CompletedTask;
    }

    public async Task EnsureStructureEditableAsync(Phase phase, string tournamentId)
    {
        if (phase.Status == PhaseStatus.Completed)
            throw new PhaseStateException(phase.Name, "modify structure of",
                "phase is completed");

        if (await _gameRepository.GamesExistForPhaseAsync(tournamentId, phase.Id))
            throw new PhaseStateException(phase.Name, "modify structure of",
                "games have been generated. Delete the games first");
    }

    public async Task EnsureDeletableAsync(Phase phase, string tournamentId)
    {
        if (phase.Status == PhaseStatus.Completed)
            throw new PhaseStateException(phase.Name, "delete",
                "phase is completed");

        if (await _gameRepository.GamesExistForPhaseAsync(tournamentId, phase.Id))
            throw new PhaseStateException(phase.Name, "delete",
                "games have been generated. Delete the games first");
    }

    public Task EnsureGamesDeletableAsync(Phase phase)
    {
        if (phase.Status == PhaseStatus.Completed)
            throw new PhaseStateException(phase.Name, "delete games from",
                "phase is completed. Reopen the phase first");

        return Task.CompletedTask;
    }

    public Task EnsureResultsCanBeRecordedAsync(Phase phase)
    {
        if (phase.Status == PhaseStatus.Completed)
            throw new PhaseStateException(phase.Name, "record results in",
                "phase is completed. Reopen the phase first");

        return Task.CompletedTask;
    }
}
