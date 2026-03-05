using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class GameScheduler
{
    /// <summary>
    /// Schedules games across courts and time slots.
    /// Constraints: no team plays/referees two games in the same slot, no consecutive refereeing,
    /// sequential court assignment (first available), group interleaving by provided order,
    /// round ordering for playoffs.
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
        var refereedInSlot = new Dictionary<int, HashSet<string>>();
        var courtSlotUsed = new Dictionary<(int slot, string courtId), bool>();

        var maxSlotLimit = games.Count * 10;

        foreach (var game in sorted)
        {
            var involvedTeamIds = GetInvolvedTeamIds(game);
            var scheduled = false;

            for (var slot = 0; slot < maxSlotLimit; slot++)
            {
                if (TryScheduleGameInSlot(game, slot, involvedTeamIds, courtIds, startTime, gameLengthMinutes,
                        busyInSlot, refereedInSlot, courtSlotUsed))
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
    /// Groups are ordered by the provided groupOrder list.
    /// This ensures groups are distributed evenly and playoff rounds are in order.
    /// </summary>
    internal static List<Game> SortGamesForScheduling(List<Game> games, List<string> groupOrder)
    {
        return games
            .GroupBy(g => g.Round)
            .OrderBy(r => r.Key)
            .SelectMany(roundGroup =>
            {
                // Within each round, interleave groups in the provided order
                var byGroup = roundGroup
                    .GroupBy(g => g.GroupId)
                    .OrderBy(g => groupOrder.IndexOf(g.Key) is var idx and >= 0 ? idx : int.MaxValue)
                    .Select(g => g.ToList())
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

    private static bool TryScheduleGameInSlot(
        Game game,
        int slot,
        List<string> involvedTeamIds,
        List<string> courtIds,
        DateTime startTime,
        int gameLengthMinutes,
        Dictionary<int, HashSet<string>> busyInSlot,
        Dictionary<int, HashSet<string>> refereedInSlot,
        Dictionary<(int slot, string courtId), bool> courtSlotUsed)
    {
        if (involvedTeamIds.Any(t => IsInSet(busyInSlot, slot, t)))
            return false;

        if (slot > 0 && game.RefereeTeamId is not null
            && IsInSet(refereedInSlot, slot - 1, game.RefereeTeamId))
            return false;

        var court = FindAvailableCourt(courtIds, slot, courtSlotUsed);
        if (court is null)
            return false;

        var gameStartTime = startTime.AddMinutes(slot * gameLengthMinutes);
        game.AssignSchedule(court, gameStartTime);

        courtSlotUsed[(slot, court)] = true;

        if (!busyInSlot.ContainsKey(slot))
            busyInSlot[slot] = [];
        foreach (var teamId in involvedTeamIds)
            busyInSlot[slot].Add(teamId);

        if (game.RefereeTeamId is not null)
        {
            if (!refereedInSlot.ContainsKey(slot))
                refereedInSlot[slot] = [];
            refereedInSlot[slot].Add(game.RefereeTeamId);
        }

        return true;
    }

    private static List<string> GetInvolvedTeamIds(Game game)
    {
        var ids = new List<string>();
        if (game.HomeTeamId is not null) ids.Add(game.HomeTeamId);
        if (game.AwayTeamId is not null) ids.Add(game.AwayTeamId);
        if (game.RefereeTeamId is not null) ids.Add(game.RefereeTeamId);
        return ids;
    }

    private static bool IsInSet(Dictionary<int, HashSet<string>> slotMap, int slot, string teamId)
    {
        return slotMap.TryGetValue(slot, out var teams) && teams.Contains(teamId);
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
