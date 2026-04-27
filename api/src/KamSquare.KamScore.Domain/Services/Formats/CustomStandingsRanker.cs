using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public static class CustomStandingsRanker
{
    /// <summary>
    /// Builds standings from the owner-entered <see cref="Group.ManualStandingOrder"/>.
    /// Position is derived from the order's index (index i → position i+1).
    /// Stats fields (Points, Wins, SetDifference, etc.) are left blank.
    /// Returns an empty list when the group has no saved order.
    /// </summary>
    public static List<Standing> Calculate(Group group)
    {
        if (group.ManualStandingOrder.Count == 0)
            return [];

        return group.ManualStandingOrder
            .Select((teamId, index) => new Standing(
                TeamId: teamId,
                Position: index + 1,
                GamesPlayed: 0,
                Wins: 0,
                Draws: 0,
                Losses: 0,
                Points: null,
                SetsWon: null,
                SetsLost: null,
                SetDifference: null,
                PointsWon: null,
                PointsLost: null,
                PointDifference: null))
            .ToList();
    }

    /// <summary>
    /// Cross-group ranking for Custom format preserves each team's manual position.
    /// Ties across groups are resolved by the order in which standings were supplied
    /// (stable sort).
    /// </summary>
    public static List<Standing> RankCrossGroup(List<Standing> standings)
    {
        return standings
            .Select((s, index) => (s, index))
            .OrderBy(x => x.s.Position)
            .ThenBy(x => x.index)
            .Select(x => x.s)
            .ToList();
    }
}
