using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

/// <summary>
/// Canonical ordering for any collection of <see cref="Game"/>s returned to
/// a client or rendered in a list. Apply this once at every API boundary
/// that exposes games — the SPA renders games in the order it receives them.
/// </summary>
public static class GameSortingExtensions
{
    /// <summary>
    /// Orders games for display in play order:
    /// <list type="number">
    /// <item><c>StartTime</c> ascending — scheduled games first, in clock order.</item>
    /// <item><c>Round</c> ascending — fallback for unscheduled games so round 1 appears before round 2, etc.</item>
    /// <item>Resolved-side count descending — within a round, games with both
    /// teams known come before games that still reference placeholders. This
    /// is what realises the "bye seed plays the last game of the next round"
    /// rule for unscheduled brackets; for scheduled games the equivalent
    /// preference is already baked into <c>StartTime</c> by
    /// <see cref="GameScheduler"/>, so this acts as a no-op tiebreaker.</item>
    /// <item><c>Label</c> ordinal — deterministic tiebreak that preserves
    /// bracket-position seeding within a round (QF1 before QF2, etc.).</item>
    /// </list>
    /// </summary>
    public static IOrderedEnumerable<Game> InScheduleOrder(this IEnumerable<Game> games)
    {
        return games
            .OrderBy(g => g.StartTime ?? DateTime.MaxValue)
            .ThenBy(g => g.Round)
            .ThenByDescending(ResolvedSideCount)
            .ThenBy(g => g.Label, StringComparer.Ordinal);
    }

    private static int ResolvedSideCount(Game game) =>
        (game.HomeTeamId is not null ? 1 : 0) + (game.AwayTeamId is not null ? 1 : 0);
}
