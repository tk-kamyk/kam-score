using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Application.Services;

/// <summary>
/// Orchestrates within-phase playoff bracket advancement when a game result is recorded.
/// Fetches sibling games, delegates to <see cref="BracketUtilities.ResolveAdvancement"/>,
/// and persists the updated games. Extracted from the game-result endpoint so the
/// endpoint handler only deals with HTTP/validation concerns.
/// </summary>
public class BracketAdvancementService
{
    private readonly IGameRepository _gameRepository;

    public BracketAdvancementService(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task ResolveAsync(string tournamentId, Game completedGame)
    {
        if (completedGame.Label is null)
            return;

        var sameGroupGames = (await _gameRepository.GetByPhaseIdAsync(tournamentId, completedGame.PhaseId))
            .Where(g => g.GroupId == completedGame.GroupId)
            .ToList();

        var advancedGames = BracketUtilities.ResolveAdvancement(completedGame, sameGroupGames);
        if (advancedGames.Count == 0)
            return;

        await Task.WhenAll(advancedGames.Select(g => _gameRepository.UpdateAsync(g)));
    }
}
