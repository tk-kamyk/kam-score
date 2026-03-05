using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class PlaceholderResolver
{
    /// <summary>
    /// Resolves placeholder teams to real teams by swapping IDs in games and group assignments.
    /// Placeholder teams are matched to real teams by seed order.
    /// </summary>
    public static List<Game> Resolve(
        List<Game> games,
        Phase nextPhase,
        List<Team> placeholderTeams,
        List<string> seededRealTeamIds)
    {
        var mapping = BuildResolveMapping(placeholderTeams, seededRealTeamIds);
        var modifiedGames = SwapTeamIds(games, mapping);

        foreach (var group in nextPhase.Groups)
        {
            group.ReplaceTeamIds(mapping);
        }

        foreach (var placeholder in placeholderTeams)
        {
            if (mapping.TryGetValue(placeholder.Id, out var realTeamId))
            {
                placeholder.ResolvedTeamId = realTeamId;
                placeholder.LastModified = DateTime.UtcNow;
            }
        }

        return modifiedGames;
    }

    /// <summary>
    /// Unresolves placeholder teams by swapping real team IDs back to placeholder IDs in games and group assignments.
    /// </summary>
    public static List<Game> Unresolve(
        List<Game> games,
        Phase nextPhase,
        List<Team> placeholderTeams)
    {
        var mapping = BuildUnresolveMapping(placeholderTeams);
        var modifiedGames = SwapTeamIds(games, mapping);

        foreach (var group in nextPhase.Groups)
        {
            group.ReplaceTeamIds(mapping);
        }

        foreach (var placeholder in placeholderTeams)
        {
            placeholder.ResolvedTeamId = null;
            placeholder.LastModified = DateTime.UtcNow;
        }

        return modifiedGames;
    }

    /// <summary>
    /// Builds mapping: placeholder team ID → real team ID, ordered by seed.
    /// </summary>
    private static Dictionary<string, string> BuildResolveMapping(
        List<Team> placeholderTeams,
        List<string> seededRealTeamIds)
    {
        var mapping = new Dictionary<string, string>();
        var ordered = placeholderTeams.OrderBy(t => t.Seed).ToList();

        for (var i = 0; i < ordered.Count && i < seededRealTeamIds.Count; i++)
        {
            mapping[ordered[i].Id] = seededRealTeamIds[i];
        }

        return mapping;
    }

    /// <summary>
    /// Builds reverse mapping: real team ID → placeholder team ID, from resolved placeholder teams.
    /// </summary>
    private static Dictionary<string, string> BuildUnresolveMapping(List<Team> placeholderTeams)
    {
        return placeholderTeams
            .Where(t => t.ResolvedTeamId is not null)
            .ToDictionary(t => t.ResolvedTeamId!, t => t.Id);
    }

    /// <summary>
    /// Swaps team IDs in games according to the mapping.
    /// Returns only the games that were modified.
    /// </summary>
    private static List<Game> SwapTeamIds(List<Game> games, Dictionary<string, string> mapping)
    {
        var modified = new List<Game>();

        foreach (var game in games)
        {
            var changed = false;

            if (game.HomeTeamId is not null && mapping.TryGetValue(game.HomeTeamId, out var newHomeId))
            {
                game.HomeTeamId = newHomeId;
                changed = true;
            }

            if (game.AwayTeamId is not null && mapping.TryGetValue(game.AwayTeamId, out var newAwayId))
            {
                game.AwayTeamId = newAwayId;
                changed = true;
            }

            if (game.RefereeTeamId is not null && mapping.TryGetValue(game.RefereeTeamId, out var newRefId))
            {
                game.RefereeTeamId = newRefId;
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
