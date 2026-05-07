using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class CourtAssigner
{
    /// <summary>
    /// Assigns one court per group: the group at index i in <paramref name="groupOrder"/>
    /// receives the court at index i in <paramref name="courtIds"/>. When there are more
    /// groups than courts, courts are reused (round-robin via modulo). Used when full
    /// scheduling is skipped (no phase start time / tournament game length) but games
    /// still need a court so they appear under the right court in the UI.
    /// </summary>
    public static void AssignByGroup(List<Game> games, List<string> courtIds, List<string> groupOrder)
    {
        if (games.Count == 0 || courtIds.Count == 0 || groupOrder.Count == 0)
            return;

        var courtByGroup = new Dictionary<string, string>(groupOrder.Count);
        for (var i = 0; i < groupOrder.Count; i++)
            courtByGroup[groupOrder[i]] = courtIds[i % courtIds.Count];

        foreach (var game in games)
        {
            if (courtByGroup.TryGetValue(game.GroupId, out var courtId))
                game.AssignCourt(courtId);
        }
    }
}
