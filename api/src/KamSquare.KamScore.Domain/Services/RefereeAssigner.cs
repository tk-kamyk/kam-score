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
        int? gameLengthMinutes)
    {
        // Find the level of the target game's group
        var targetGroup = phaseGroups.FirstOrDefault(g => g.Id == targetGame.GroupId);
        var targetLevelId = targetGroup?.LevelId;

        // Collect all teams from groups in the same level (or all groups if no levels)
        var levelGroups = targetLevelId is not null
            ? phaseGroups.Where(g => g.LevelId == targetLevelId)
            : phaseGroups;

        var allTeamIds = levelGroups.SelectMany(g => g.TeamIds).ToHashSet();

        // When the game has no start time or the tournament has no game length, we
        // cannot reason about availability windows — return every level/group team
        // (minus the two playing teams) plus any earlier-round bracket placeholders.
        if (targetGame.StartTime is null || gameLengthMinutes is null or <= 0)
        {
            var unscheduledRealCandidates = allTeamIds
                .Where(t => t != targetGame.HomeTeamId && t != targetGame.AwayTeamId);
            var unscheduledPlaceholders = BuildBracketPlaceholders(targetGame, allPhaseGames);
            return unscheduledRealCandidates.Concat(unscheduledPlaceholders).ToList();
        }

        // Compute slot numbers
        var scheduledGames = allPhaseGames.Where(g => g.StartTime.HasValue).ToList();
        if (scheduledGames.Count == 0)
            return [];

        var baseTime = scheduledGames.Min(g => g.StartTime!.Value);
        var slotMinutes = gameLengthMinutes.Value;
        int SlotOf(DateTime time) => (int)Math.Round((time - baseTime).TotalMinutes / slotMinutes);

        var targetSlot = SlotOf(targetGame.StartTime.Value);
        var nextSlot = targetSlot + 1;

        // Build busy sets for target slot (playing + refereeing) and next slot (playing only)
        // These track both real team IDs and placeholder strings
        var busyInTargetSlot = new HashSet<string>();
        var playingInNextSlot = new HashSet<string>();

        foreach (var game in scheduledGames)
        {
            var slot = SlotOf(game.StartTime!.Value);

            if (slot == targetSlot)
            {
                if (game.HomeTeamId is not null) busyInTargetSlot.Add(game.HomeTeamId);
                if (game.AwayTeamId is not null) busyInTargetSlot.Add(game.AwayTeamId);
                if (game.HomeTeamPlaceholder is not null) busyInTargetSlot.Add(game.HomeTeamPlaceholder);
                if (game.AwayTeamPlaceholder is not null) busyInTargetSlot.Add(game.AwayTeamPlaceholder);

                // Skip referee of target game so current referee remains a candidate for re-assignment
                if (game.Id != targetGame.Id)
                {
                    if (game.RefereeTeamId is not null) busyInTargetSlot.Add(game.RefereeTeamId);
                    if (game.RefereeTeamPlaceholder is not null) busyInTargetSlot.Add(game.RefereeTeamPlaceholder);
                }
            }

            if (slot == nextSlot)
            {
                if (game.HomeTeamId is not null) playingInNextSlot.Add(game.HomeTeamId);
                if (game.AwayTeamId is not null) playingInNextSlot.Add(game.AwayTeamId);
                if (game.HomeTeamPlaceholder is not null) playingInNextSlot.Add(game.HomeTeamPlaceholder);
                if (game.AwayTeamPlaceholder is not null) playingInNextSlot.Add(game.AwayTeamPlaceholder);
            }
        }

        var realCandidates = allTeamIds
            .Where(t => !busyInTargetSlot.Contains(t))
            .Where(t => !playingInNextSlot.Contains(t));

        // Collect bracket placeholders from earlier-round games in the same group
        var placeholderCandidates = BuildBracketPlaceholders(targetGame, allPhaseGames)
            .Where(p => !busyInTargetSlot.Contains(p))
            .Where(p => !playingInNextSlot.Contains(p));

        return realCandidates.Concat(placeholderCandidates).ToList();
    }

    /// <summary>
    /// Extracts "Winner {label}" and "Loser {label}" placeholders from all other
    /// labeled games in the same group. The busy/playing exclusion rules handle
    /// filtering out any that aren't actually available.
    /// </summary>
    private static HashSet<string> BuildBracketPlaceholders(Game targetGame, List<Game> allPhaseGames)
    {
        var placeholders = new HashSet<string>();

        var earlierGames = allPhaseGames
            .Where(g => g.GroupId == targetGame.GroupId && g.Round <= targetGame.Round && g.Id != targetGame.Id && g.Label is not null);

        foreach (var game in earlierGames)
        {
            placeholders.Add($"Winner {game.Label}");
            placeholders.Add($"Loser {game.Label}");
        }

        return placeholders;
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
