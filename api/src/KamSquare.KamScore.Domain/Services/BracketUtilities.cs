using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class BracketUtilities
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

            if (game.RefereeTeamPlaceholder == winnerPlaceholder && winnerId is not null)
            {
                game.RefereeTeamId = winnerId;
                changed = true;
            }

            if (game.RefereeTeamPlaceholder == loserPlaceholder && loserId is not null)
            {
                game.RefereeTeamId = loserId;
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

    internal static int NextPowerOfTwo(int n)
    {
        var power = 1;
        while (power < n)
            power *= 2;
        return power;
    }

    /// <summary>
    /// Builds bracket ordering so that seed 1 plays seed N, seed 2 plays N-1, etc.
    /// Returns an array where positions[i] = seed index (0-based).
    /// </summary>
    internal static int[] BuildBracketOrder(int bracketSize)
    {
        // Start with [0, 1] and recursively build
        if (bracketSize == 2)
            return [0, 1];

        var halfOrder = BuildBracketOrder(bracketSize / 2);
        var order = new int[bracketSize];

        for (var i = 0; i < halfOrder.Length; i++)
        {
            order[i * 2] = halfOrder[i];
            order[i * 2 + 1] = bracketSize - 1 - halfOrder[i];
        }

        return order;
    }

    internal static string[] GetRoundNames(int totalRounds)
    {
        var names = new string[totalRounds];
        for (var i = 0; i < totalRounds; i++)
        {
            var roundFromEnd = totalRounds - i;
            names[i] = roundFromEnd switch
            {
                1 => "F",
                2 => "SF",
                3 => "QF",
                _ => $"R{i + 1}"
            };
        }
        return names;
    }

    internal static string GetMatchLabel(string roundName, int matchNumber)
    {
        if (roundName == "F")
            return "Final";
        return $"{roundName}{matchNumber}";
    }
}
