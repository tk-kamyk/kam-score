using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class GameScheduler
{
    /// <summary>
    /// Schedules games across courts and time slots.
    /// Constraints: no team plays/referees two games in the same slot, no consecutive refereeing,
    /// uniform court distribution, group interleaving, round ordering for playoffs.
    /// </summary>
    public static List<Game> Schedule(
        List<Game> games,
        List<string> courtIds,
        DateTime startTime,
        int gameLengthMinutes)
    {
        if (games.Count == 0 || courtIds.Count == 0)
            return games;

        // Sort games: by round, then interleave groups within each round
        var sorted = SortGamesForScheduling(games);

        // Track team activity per time slot for conflict checking
        var busyInSlot = new Dictionary<int, HashSet<string>>();
        var refereedInSlot = new Dictionary<int, HashSet<string>>();
        var courtSlotUsed = new Dictionary<(int slot, string courtId), bool>();

        // Track court assignment counts for uniform distribution
        var courtGameCount = courtIds.ToDictionary(c => c, _ => 0);
        var courtIndex = 0;

        foreach (var game in sorted)
        {
            var involvedTeamIds = GetInvolvedTeamIds(game);

            // Find earliest valid time slot
            for (var slot = 0; ; slot++)
            {
                // Check same-slot conflict: no team can play/referee two games at the same time
                if (involvedTeamIds.Any(t => IsInSet(busyInSlot, slot, t)))
                    continue;

                // Check consecutive referee: referee must not have refereed in previous slot
                if (slot > 0 && game.RefereeTeamId is not null
                    && IsInSet(refereedInSlot, slot - 1, game.RefereeTeamId))
                    continue;

                // Find an available court in this slot (prefer least-used for uniformity)
                var court = FindAvailableCourt(courtIds, slot, courtSlotUsed, courtGameCount, ref courtIndex);
                if (court is null)
                    continue;

                // Assign schedule
                var gameStartTime = startTime.AddMinutes(slot * gameLengthMinutes);
                game.AssignSchedule(court, gameStartTime);

                // Record occupancy
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

                courtGameCount[court]++;
                break;
            }
        }

        return games;
    }

    /// <summary>
    /// Sorts games by round (ascending), interleaving groups within each round.
    /// This ensures groups are distributed evenly and playoff rounds are in order.
    /// </summary>
    internal static List<Game> SortGamesForScheduling(List<Game> games)
    {
        return games
            .GroupBy(g => g.Round)
            .OrderBy(r => r.Key)
            .SelectMany(roundGroup =>
            {
                // Within each round, interleave groups
                var byGroup = roundGroup
                    .GroupBy(g => g.GroupId)
                    .OrderBy(g => g.Key)
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
        Dictionary<(int slot, string courtId), bool> courtSlotUsed,
        Dictionary<string, int> courtGameCount,
        ref int courtIndex)
    {
        // Try courts in round-robin order for uniform distribution
        for (var i = 0; i < courtIds.Count; i++)
        {
            var idx = (courtIndex + i) % courtIds.Count;
            var courtId = courtIds[idx];

            if (!courtSlotUsed.ContainsKey((slot, courtId)))
            {
                courtIndex = (idx + 1) % courtIds.Count;
                return courtId;
            }
        }

        return null;
    }
}
