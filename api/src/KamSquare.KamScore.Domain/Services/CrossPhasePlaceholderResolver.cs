using System.Text.RegularExpressions;
using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static partial class CrossPhasePlaceholderResolver
{
    [GeneratedRegex(@" - Seed (\d+)$")]
    private static partial Regex SeedNumberRegex();

    /// <summary>
    /// Resolves cross-phase placeholders in games by replacing them with real team IDs.
    /// Placeholders matching the source phase name are resolved using the seed-to-team mapping.
    /// Placeholder strings are kept intact for potential re-resolution.
    /// Returns the list of games that were modified.
    /// </summary>
    public static List<Game> Resolve(
        List<Game> games,
        Dictionary<int, string> seedToTeamId,
        string sourcePhaseName)
    {
        var modified = new List<Game>();

        foreach (var game in games)
        {
            var changed = false;

            if (IsMatchingPlaceholder(game.HomeTeamPlaceholder, sourcePhaseName))
            {
                var seedNumber = ExtractSeedNumber(game.HomeTeamPlaceholder!);
                if (seedNumber.HasValue && seedToTeamId.TryGetValue(seedNumber.Value, out var homeTeamId))
                {
                    game.HomeTeamId = homeTeamId;
                    changed = true;
                }
            }

            if (IsMatchingPlaceholder(game.AwayTeamPlaceholder, sourcePhaseName))
            {
                var seedNumber = ExtractSeedNumber(game.AwayTeamPlaceholder!);
                if (seedNumber.HasValue && seedToTeamId.TryGetValue(seedNumber.Value, out var awayTeamId))
                {
                    game.AwayTeamId = awayTeamId;
                    changed = true;
                }
            }

            if (changed)
            {
                game.LastModified = DateTime.UtcNow;
                modified.Add(game);
            }
        }

        return modified;
    }

    /// <summary>
    /// Unresolves cross-phase placeholders by clearing team IDs for games that have
    /// matching cross-phase placeholder strings. Placeholder strings are kept intact.
    /// Returns the list of games that were modified.
    /// </summary>
    public static List<Game> Unresolve(List<Game> games, string sourcePhaseName)
    {
        var modified = new List<Game>();

        foreach (var game in games)
        {
            var changed = false;

            if (IsMatchingPlaceholder(game.HomeTeamPlaceholder, sourcePhaseName))
            {
                game.HomeTeamId = null;
                changed = true;
            }

            if (IsMatchingPlaceholder(game.AwayTeamPlaceholder, sourcePhaseName))
            {
                game.AwayTeamId = null;
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

    /// <summary>
    /// Builds a seed-to-team-ID mapping from a seeded team list.
    /// Seed 1 = first element, Seed 2 = second, etc.
    /// </summary>
    public static Dictionary<int, string> BuildSeedMapping(List<string> seededTeamIds)
    {
        return seededTeamIds
            .Select((teamId, index) => (SeedNumber: index + 1, TeamId: teamId))
            .ToDictionary(x => x.SeedNumber, x => x.TeamId);
    }

    private static bool IsMatchingPlaceholder(string? placeholder, string sourcePhaseName)
    {
        return placeholder is not null
               && placeholder.StartsWith(sourcePhaseName, StringComparison.Ordinal)
               && SeedNumberRegex().IsMatch(placeholder);
    }

    private static int? ExtractSeedNumber(string placeholder)
    {
        var match = SeedNumberRegex().Match(placeholder);
        return match.Success && int.TryParse(match.Groups[1].Value, out var seedNumber)
            ? seedNumber
            : null;
    }
}
