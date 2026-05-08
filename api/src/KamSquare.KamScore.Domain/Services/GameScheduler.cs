using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class GameScheduler
{
    /// <summary>
    /// Schedules games across courts and time slots.
    /// Constraints: no team plays two games in the same slot, teams must have a free slot
    /// before playing, sequential court assignment (first available), group interleaving
    /// by provided order, round ordering for playoffs.
    /// Referees are not assigned during scheduling — use RefereeAssigner after scheduling.
    /// </summary>
    public static List<Game> Schedule(
        List<Game> games,
        List<string> courtIds,
        List<string> groupOrder,
        DateTime startTime,
        int gameLengthMinutes)
    {
        if (games.Count == 0 || courtIds.Count == 0)
            return games;

        // Sort games: by round, then interleave groups within each round
        var sorted = SortGamesForScheduling(games, groupOrder);

        // Track team activity per time slot for conflict checking
        var busyInSlot = new Dictionary<int, HashSet<string>>();
        var courtSlotUsed = new Dictionary<(int slot, string courtId), bool>();

        var maxSlotLimit = games.Count * 10;

        foreach (var game in sorted)
        {
            var playingTeamIds = GetPlayingTeamIds(game);
            var scheduled = false;

            for (var slot = 0; slot < maxSlotLimit; slot++)
            {
                if (TryScheduleGameInSlot(game, slot, playingTeamIds, courtIds, startTime, gameLengthMinutes,
                        busyInSlot, courtSlotUsed))
                {
                    scheduled = true;
                    break;
                }
            }

            if (!scheduled)
                throw new InvalidOperationException(
                    $"Unable to schedule game {game.Id} within {maxSlotLimit} time slots.");
        }

        return games;
    }

    /// <summary>
    /// Sorts games by round (ascending), interleaving groups within each round.
    /// Within a (round, group) bucket, games whose teams are fully resolved
    /// (both <c>HomeTeamId</c> and <c>AwayTeamId</c> set) are placed before
    /// games that still reference placeholders.
    /// </summary>
    internal static List<Game> SortGamesForScheduling(List<Game> games, List<string> groupOrder)
    {
        return games
            .GroupBy(g => g.Round)
            .OrderBy(r => r.Key)
            .SelectMany(roundGroup =>
            {
                var byGroup = roundGroup
                    .GroupBy(g => g.GroupId)
                    .OrderBy(g => groupOrder.IndexOf(g.Key) is var idx and >= 0 ? idx : int.MaxValue)
                    .Select(g => g.OrderByDescending(ResolvedSideCount).ToList())
                    .ToList();

                var interleaved = new List<Game>();
                var maxGames = byGroup.Max(g => g.Count);

                for (var i = 0; i < maxGames; i++)
                {
                    foreach (var groupGames in byGroup)
                    {
                        if (i < groupGames.Count)
                            interleaved.Add(groupGames[i]);
                    }
                }

                return interleaved;
            })
            .ToList();
    }

    private static int ResolvedSideCount(Game game) =>
        (game.HomeTeamId is not null ? 1 : 0) + (game.AwayTeamId is not null ? 1 : 0);

    private static bool TryScheduleGameInSlot(
        Game game,
        int slot,
        List<string> playingTeamIds,
        List<string> courtIds,
        DateTime startTime,
        int gameLengthMinutes,
        Dictionary<int, HashSet<string>> busyInSlot,
        Dictionary<(int slot, string courtId), bool> courtSlotUsed)
    {
        // No team can play two games in the same slot
        if (playingTeamIds.Any(t => busyInSlot.ContainsTeam(slot, t)))
            return false;

        // Teams must have a free slot before playing (no activity in preceding slot)
        if (slot > 0 && playingTeamIds.Any(t => busyInSlot.ContainsTeam(slot - 1, t)))
            return false;

        // Teams must not already be scheduled in the next slot (bidirectional rest guarantee).
        // Safe because games are processed in round/group order and earlier slots fill first,
        // so any game already in slot+1 is a real conflict.
        if (playingTeamIds.Any(t => busyInSlot.ContainsTeam(slot + 1, t)))
            return false;

        var court = FindAvailableCourt(courtIds, slot, courtSlotUsed);
        if (court is null)
            return false;

        var gameStartTime = startTime.AddMinutes(slot * gameLengthMinutes);
        game.AssignSchedule(court, gameStartTime);

        courtSlotUsed[(slot, court)] = true;

        if (!busyInSlot.ContainsKey(slot))
            busyInSlot[slot] = [];
        foreach (var teamId in playingTeamIds)
            busyInSlot[slot].Add(teamId);

        return true;
    }

    private static List<string> GetPlayingTeamIds(Game game)
    {
        var ids = new List<string>();
        if (game.HomeTeamId is not null) ids.Add(game.HomeTeamId);
        if (game.AwayTeamId is not null) ids.Add(game.AwayTeamId);
        return ids;
    }

    private static string? FindAvailableCourt(
        List<string> courtIds,
        int slot,
        Dictionary<(int slot, string courtId), bool> courtSlotUsed)
    {
        // Always prefer the first available court in the provided order
        for (var i = 0; i < courtIds.Count; i++)
        {
            if (!courtSlotUsed.ContainsKey((slot, courtIds[i])))
                return courtIds[i];
        }

        return null;
    }
}
