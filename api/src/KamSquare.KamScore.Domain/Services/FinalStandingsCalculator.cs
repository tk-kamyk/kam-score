using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services;

public static class FinalStandingsCalculator
{
    public record FinalStandingsResult(bool Provisional, List<FinalStanding> Standings);

    public static FinalStandingsResult Calculate(
        List<Phase> phases,
        List<Game> allGames,
        List<Team> allTeams)
    {
        if (phases.Count == 0)
            return new FinalStandingsResult(false, []);

        var realTeams = allTeams.Where(t => !t.IsPlaceholder).ToList();
        var teamNameLookup = realTeams.ToDictionary(t => t.Id, t => t.Name);

        // Check if any games exist at all
        var completedGames = allGames.Where(g => g.Status == GameStatus.Completed).ToList();
        if (completedGames.Count == 0)
            return new FinalStandingsResult(false, []);

        var orderedPhases = phases.OrderBy(p => p.Order).ToList();
        var provisional = orderedPhases.Any(p => p.Status != PhaseStatus.Completed);

        var hasLevels = orderedPhases.Any(p => p.Levels.Count > 0);

        if (hasLevels)
            return CalculateWithLevels(orderedPhases, allGames, teamNameLookup, provisional);

        return CalculateFlat(orderedPhases, allGames, teamNameLookup, provisional);
    }

    private static FinalStandingsResult CalculateFlat(
        List<Phase> orderedPhases,
        List<Game> allGames,
        Dictionary<string, string> teamNameLookup,
        bool provisional)
    {
        var groupsForPhase = orderedPhases.ToDictionary(p => p.Id, p => p.Groups.ToList());
        var globalPositioned = new HashSet<string>();
        var standings = CalculateForTeamPool(orderedPhases, allGames, teamNameLookup, null, groupsForPhase, globalPositioned);
        return new FinalStandingsResult(provisional, standings);
    }

    private static FinalStandingsResult CalculateWithLevels(
        List<Phase> orderedPhases,
        List<Game> allGames,
        Dictionary<string, string> teamNameLookup,
        bool provisional)
    {
        var rootLeveledPhase = orderedPhases.First(p => p.Levels.Count > 0);
        var allStandings = new List<FinalStanding>();
        var globalPositionedTeams = new HashSet<string>();

        foreach (var rootLevel in rootLeveledPhase.Levels.OrderBy(l => l.Order))
        {
            var groupsForPhase = BuildGroupsForRootLevel(orderedPhases, rootLeveledPhase, rootLevel);
            var levelStandings = CalculateForTeamPool(
                orderedPhases, allGames, teamNameLookup, rootLevel.Name, groupsForPhase, globalPositionedTeams);
            allStandings.AddRange(levelStandings);
        }

        return new FinalStandingsResult(provisional, allStandings);
    }

    /// <summary>
    /// Builds a per-phase group mapping for a given root level.
    /// Handles phases with no levels, same levels, or more levels (split factor).
    /// </summary>
    private static Dictionary<string, List<Group>> BuildGroupsForRootLevel(
        List<Phase> orderedPhases,
        Phase rootLeveledPhase,
        Level rootLevel)
    {
        var rootLevelCount = rootLeveledPhase.Levels.Count;
        var result = new Dictionary<string, List<Group>>();

        foreach (var phase in orderedPhases)
        {
            if (phase.Levels.Count == 0)
            {
                // Pre-level phase: include all groups
                result[phase.Id] = phase.Groups.ToList();
                continue;
            }

            if (phase.Id == rootLeveledPhase.Id)
            {
                // Root leveled phase: match directly
                result[phase.Id] = phase.Groups.Where(g => g.LevelId == rootLevel.Id).ToList();
                continue;
            }

            // Phase with different level count: compute corresponding levels by order
            var phaseLevelCount = phase.Levels.Count;
            var overallFactor = phaseLevelCount / rootLevelCount;
            var startOrder = (rootLevel.Order - 1) * overallFactor + 1;
            var endOrder = rootLevel.Order * overallFactor;

            var matchingLevelIds = phase.Levels
                .Where(l => l.Order >= startOrder && l.Order <= endOrder)
                .Select(l => l.Id)
                .ToHashSet();

            result[phase.Id] = phase.Groups.Where(g => g.LevelId is not null && matchingLevelIds.Contains(g.LevelId)).ToList();
        }

        return result;
    }

    private static List<FinalStanding> CalculateForTeamPool(
        List<Phase> orderedPhases,
        List<Game> allGames,
        Dictionary<string, string> teamNameLookup,
        string? levelName,
        Dictionary<string, List<Group>> groupsForPhase,
        HashSet<string> globalPositionedTeams)
    {
        var gamesByPhase = allGames.ToLookup(g => g.PhaseId);

        // Collect assigned team IDs from each phase
        var assignedTeams = new Dictionary<string, HashSet<string>>();
        foreach (var phase in orderedPhases)
        {
            var teamIds = groupsForPhase[phase.Id]
                .SelectMany(g => g.TeamIds)
                .Where(id => teamNameLookup.ContainsKey(id))
                .ToHashSet();

            assignedTeams[phase.Id] = teamIds;
        }

        // Work from last phase to first, assigning positions
        var result = new List<FinalStanding>();
        var positionedTeams = new HashSet<string>(globalPositionedTeams);
        var nextPosition = 1;

        for (var i = orderedPhases.Count - 1; i >= 0; i--)
        {
            var phase = orderedPhases[i];
            if (assignedTeams[phase.Id].Count == 0) continue;

            var phaseGames = gamesByPhase[phase.Id].ToList();
            var groups = groupsForPhase[phase.Id];

            var groupStandings = CalculateGroupStandings(groups, phaseGames, phase.Format, teamNameLookup);

            if (i == orderedPhases.Count - 1)
                AssignLastPhasePositions(phase, groups, groupStandings, teamNameLookup, levelName, result, positionedTeams, ref nextPosition);
            else
                AssignEarlierPhasePositions(phase, groups, groupStandings, teamNameLookup, levelName, result, positionedTeams, ref nextPosition);
        }

        // Track all positioned teams globally to avoid double-counting across level iterations
        foreach (var standing in result)
        {
            globalPositionedTeams.Add(standing.TeamId);
        }

        return result;
    }

    private static List<(string GroupId, List<Standing> Standings)> CalculateGroupStandings(
        List<Group> groups,
        List<Game> phaseGames,
        PhaseFormat format,
        Dictionary<string, string> teamNameLookup)
    {
        return groups
            .Select(g =>
            {
                var groupGames = phaseGames.Where(game => game.GroupId == g.Id).ToList();
                var realTeamIds = g.TeamIds.Where(id => teamNameLookup.ContainsKey(id)).ToList();
                return (g.Id, StandingsCalculator.Calculate(format, groupGames, realTeamIds));
            })
            .ToList();
    }

    private static void AssignLastPhasePositions(
        Phase phase,
        List<Group> groups,
        List<(string GroupId, List<Standing> Standings)> groupStandings,
        Dictionary<string, string> teamNameLookup,
        string? levelName,
        List<FinalStanding> result,
        HashSet<string> positionedTeams,
        ref int nextPosition)
    {
        var ranked = RankRespectingLevels(phase, groups, groupStandings, phase.Format);
        foreach (var standing in ranked)
        {
            if (positionedTeams.Contains(standing.TeamId)) continue;
            if (!teamNameLookup.TryGetValue(standing.TeamId, out var name)) continue;

            result.Add(new FinalStanding(nextPosition, standing.TeamId, name, levelName));
            positionedTeams.Add(standing.TeamId);
            nextPosition++;
        }
    }

    private static void AssignEarlierPhasePositions(
        Phase phase,
        List<Group> groups,
        List<(string GroupId, List<Standing> Standings)> groupStandings,
        Dictionary<string, string> teamNameLookup,
        string? levelName,
        List<FinalStanding> result,
        HashSet<string> positionedTeams,
        ref int nextPosition)
    {
        var advancingTeamIds = GetAdvancingTeamIds(phase, groupStandings);
        var nonAdvancing = RankRespectingLevels(phase, groups, groupStandings, phase.Format)
            .Where(s => !advancingTeamIds.Contains(s.TeamId))
            .Where(s => !positionedTeams.Contains(s.TeamId))
            .ToList();

        foreach (var standing in nonAdvancing)
        {
            if (!teamNameLookup.TryGetValue(standing.TeamId, out var name)) continue;

            result.Add(new FinalStanding(nextPosition, standing.TeamId, name, levelName));
            positionedTeams.Add(standing.TeamId);
            nextPosition++;
        }
    }

    private static HashSet<string> GetAdvancingTeamIds(
        Phase phase,
        List<(string GroupId, List<Standing> Standings)> groupStandings)
    {
        if (!phase.HasProgressionConfig)
            return [];

        if (phase.GroupWinners is 0 && phase.TotalTeamsProceeding is null or 0)
            return [];

        var qualifyingIds = PhaseAdvancementCalculator.CalculateQualifyingTeamIds(phase, groupStandings);
        return qualifyingIds.ToHashSet();
    }

    private static List<Standing> RankRespectingLevels(
        Phase phase,
        List<Group> groups,
        List<(string GroupId, List<Standing> Standings)> groupStandings,
        PhaseFormat format)
    {
        if (phase.Levels.Count == 0)
            return RankCrossGroup(groupStandings, format);

        var result = new List<Standing>();
        var relevantLevels = phase.Levels
            .Where(l => groups.Any(g => g.LevelId == l.Id))
            .OrderBy(l => l.Order);

        foreach (var level in relevantLevels)
        {
            var levelGroupIds = groups
                .Where(g => g.LevelId == level.Id)
                .Select(g => g.Id)
                .ToHashSet();
            var levelStandings = groupStandings
                .Where(gs => levelGroupIds.Contains(gs.GroupId))
                .ToList();
            result.AddRange(RankCrossGroup(levelStandings, format));
        }

        return result;
    }

    private static List<Standing> RankCrossGroup(
        List<(string GroupId, List<Standing> Standings)> groupStandings,
        PhaseFormat format)
    {
        var all = groupStandings.SelectMany(gs => gs.Standings);

        if (format is PhaseFormat.PlayoffElimination or PhaseFormat.PlayoffWithPlacement)
        {
            return all
                .OrderBy(s => s.Position)
                .ThenByDescending(s => s.Wins)
                .ToList();
        }

        return all
            .OrderByDescending(s => s.Points ?? 0)
            .ThenByDescending(s => s.SetDifference ?? 0)
            .ThenByDescending(s => s.PointDifference ?? 0)
            .ThenByDescending(s => s.Wins)
            .ThenBy(s => s.Losses)
            .ToList();
    }
}
