using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class BracketAdvancementService
{
    /// <summary>
    /// Given a completed game and all games in the same group,
    /// resolves placeholder references in downstream games.
    /// Returns the list of games that were modified (need to be persisted).
    /// </summary>
    public static List<Game> ResolveAdvancement(Game completedGame, List<Game> allGamesInGroup)
    {
        var modified = new List<Game>();

        if (completedGame.Label is null)
            return modified;

        var winnerId = completedGame.GetWinnerId();
        var loserId = completedGame.GetLoserId();

        if (winnerId is null && loserId is null)
            return modified;

        var winnerPlaceholder = $"Winner {completedGame.Label}";
        var loserPlaceholder = $"Loser {completedGame.Label}";

        foreach (var game in allGamesInGroup)
        {
            if (game.Id == completedGame.Id)
                continue;

            var changed = false;

            if (game.HomeTeamPlaceholder == winnerPlaceholder && winnerId is not null)
            {
                game.HomeTeamId = winnerId;
                changed = true;
            }

            if (game.AwayTeamPlaceholder == winnerPlaceholder && winnerId is not null)
            {
                game.AwayTeamId = winnerId;
                changed = true;
            }

            if (game.HomeTeamPlaceholder == loserPlaceholder && loserId is not null)
            {
                game.HomeTeamId = loserId;
                changed = true;
            }

            if (game.AwayTeamPlaceholder == loserPlaceholder && loserId is not null)
            {
                game.AwayTeamId = loserId;
                changed = true;
            }

            if (changed)
            {
                game.LastModified = DateTime.UtcNow;
                modified.Add(game);
            }
        }

        return modified;
    }
}
