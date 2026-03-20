using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class RefereeAssigner
{
    /// <summary>
    /// Assigns referees to scheduled games. For each game, picks the team from
    /// the same group that: is not playing in this game, is not busy in the same
    /// time slot, is not playing in the next time slot, has not refereed in the
    /// previous time slot, and has fewest referee duties so far.
    /// Must be called after GameScheduler.Schedule.
    /// </summary>
    public static void Assign(List<Game> games, int gameLengthMinutes)
    {
        if (games.Count == 0)
            return;

        // Build group team rosters from game participants
        var teamsByGroup = BuildGroupTeamRosters(games);

        // Compute slot numbers from actual times using game length
        var baseTime = games.Where(g => g.StartTime.HasValue).Min(g => g.StartTime!.Value);

        int SlotOf(DateTime time) => (int)Math.Round((time - baseTime).TotalMinutes / gameLengthMinutes);

        // Build busy-in-slot map (playing teams per slot)
        var busyInSlot = new Dictionary<int, HashSet<string>>();
        foreach (var game in games.Where(g => g.StartTime.HasValue))
        {
            var slot = SlotOf(game.StartTime!.Value);
            if (!busyInSlot.ContainsKey(slot))
                busyInSlot[slot] = [];
            if (game.HomeTeamId is not null) busyInSlot[slot].Add(game.HomeTeamId);
            if (game.AwayTeamId is not null) busyInSlot[slot].Add(game.AwayTeamId);
        }

        // Track referee assignments per slot and counts
        var refereedInSlot = new Dictionary<int, HashSet<string>>();
        var refCount = new Dictionary<string, int>();

        // Process games in chronological order
        var ordered = games
            .Where(g => g.StartTime.HasValue)
            .OrderBy(g => g.StartTime!.Value)
            .ThenBy(g => g.CourtId)
            .ToList();

        foreach (var game in ordered)
        {
            if (!teamsByGroup.TryGetValue(game.GroupId, out var groupTeams))
                continue;

            var slot = SlotOf(game.StartTime!.Value);
            var nextSlot = slot + 1;
            var prevSlot = slot - 1;

            var candidate = groupTeams
                .Where(t => t != game.HomeTeamId && t != game.AwayTeamId)
                .Where(t => !busyInSlot.ContainsTeam(slot, t))
                .Where(t => !refereedInSlot.ContainsTeam(slot, t))
                .Where(t => !busyInSlot.ContainsTeam(nextSlot, t))
                .Where(t => slot == 0 || !refereedInSlot.ContainsTeam(prevSlot, t))
                .MinBy(t => refCount.GetValueOrDefault(t, 0));

            if (candidate is null)
                continue;

            game.RefereeTeamId = candidate;
            refCount[candidate] = refCount.GetValueOrDefault(candidate, 0) + 1;

            if (!refereedInSlot.ContainsKey(slot))
                refereedInSlot[slot] = [];
            refereedInSlot[slot].Add(candidate);

            // Also mark referee as busy in their slot
            if (!busyInSlot.ContainsKey(slot))
                busyInSlot[slot] = [];
            busyInSlot[slot].Add(candidate);
        }
    }

    /// <summary>
    /// Returns team IDs eligible to referee a specific game.
    /// Candidates are from the same level (or all groups if no levels),
    /// not playing/refereeing in the same slot, and not playing in the next slot.
    /// </summary>
    public static List<string> GetCandidates(
        Game targetGame,
        List<Game> allPhaseGames,
        List<Group> phaseGroups,
        int gameLengthMinutes)
    {
        if (targetGame.StartTime is null)
            return [];

        // Find the level of the target game's group
        var targetGroup = phaseGroups.FirstOrDefault(g => g.Id == targetGame.GroupId);
        var targetLevelId = targetGroup?.LevelId;

        // Collect all teams from groups in the same level (or all groups if no levels)
        var levelGroups = targetLevelId is not null
            ? phaseGroups.Where(g => g.LevelId == targetLevelId)
            : phaseGroups;

        var allTeamIds = levelGroups.SelectMany(g => g.TeamIds).ToHashSet();

        // Compute slot numbers
        var scheduledGames = allPhaseGames.Where(g => g.StartTime.HasValue).ToList();
        if (scheduledGames.Count == 0)
            return [];

        var baseTime = scheduledGames.Min(g => g.StartTime!.Value);
        int SlotOf(DateTime time) => (int)Math.Round((time - baseTime).TotalMinutes / gameLengthMinutes);

        var targetSlot = SlotOf(targetGame.StartTime.Value);
        var nextSlot = targetSlot + 1;

        // Build busy sets for target slot (playing + refereeing) and next slot (playing only)
        var busyInTargetSlot = new HashSet<string>();
        var playingInNextSlot = new HashSet<string>();

        foreach (var game in scheduledGames)
        {
            var slot = SlotOf(game.StartTime!.Value);

            if (slot == targetSlot)
            {
                if (game.HomeTeamId is not null) busyInTargetSlot.Add(game.HomeTeamId);
                if (game.AwayTeamId is not null) busyInTargetSlot.Add(game.AwayTeamId);
                if (game.RefereeTeamId is not null) busyInTargetSlot.Add(game.RefereeTeamId);
            }

            if (slot == nextSlot)
            {
                if (game.HomeTeamId is not null) playingInNextSlot.Add(game.HomeTeamId);
                if (game.AwayTeamId is not null) playingInNextSlot.Add(game.AwayTeamId);
            }
        }

        return allTeamIds
            .Where(t => !busyInTargetSlot.Contains(t))
            .Where(t => !playingInNextSlot.Contains(t))
            .ToList();
    }

    private static Dictionary<string, HashSet<string>> BuildGroupTeamRosters(List<Game> games)
    {
        var teamsByGroup = new Dictionary<string, HashSet<string>>();
        foreach (var game in games)
        {
            if (!teamsByGroup.ContainsKey(game.GroupId))
                teamsByGroup[game.GroupId] = [];
            if (game.HomeTeamId is not null) teamsByGroup[game.GroupId].Add(game.HomeTeamId);
            if (game.AwayTeamId is not null) teamsByGroup[game.GroupId].Add(game.AwayTeamId);
        }
        return teamsByGroup;
    }

}
