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
        var standings = CalculateForTeamPool(orderedPhases, allGames, teamNameLookup, null);
        return new FinalStandingsResult(provisional, standings);
    }

    private static FinalStandingsResult CalculateWithLevels(
        List<Phase> orderedPhases,
        List<Game> allGames,
        Dictionary<string, string> teamNameLookup,
        bool provisional)
    {
        // Collect all unique levels across phases (use first phase that defines levels)
        var phaseWithLevels = orderedPhases.First(p => p.Levels.Count > 0);
        var allStandings = new List<FinalStanding>();

        foreach (var level in phaseWithLevels.Levels.OrderBy(l => l.Order))
        {
            var levelStandings = CalculateForTeamPool(orderedPhases, allGames, teamNameLookup, level);
            allStandings.AddRange(levelStandings);
        }

        return new FinalStandingsResult(provisional, allStandings);
    }

    private static List<FinalStanding> CalculateForTeamPool(
        List<Phase> orderedPhases,
        List<Game> allGames,
        Dictionary<string, string> teamNameLookup,
        Level? level)
    {
        var levelName = level?.Name;
        var gamesByPhase = allGames.ToLookup(g => g.PhaseId);

        // Cache level-filtered groups per phase
        var groupsByPhase = orderedPhases.ToDictionary(
            p => p.Id,
            p => level is not null
                ? p.Groups.Where(g => g.LevelId == level.Id).ToList()
                : p.Groups.ToList());

        // Collect assigned team IDs from each phase
        var assignedTeams = new Dictionary<string, HashSet<string>>();
        foreach (var phase in orderedPhases)
        {
            var teamIds = groupsByPhase[phase.Id]
                .SelectMany(g => g.TeamIds)
                .Where(id => teamNameLookup.ContainsKey(id))
                .ToHashSet();

            assignedTeams[phase.Id] = teamIds;
        }

        // Work from last phase to first, assigning positions
        var result = new List<FinalStanding>();
        var positionedTeams = new HashSet<string>();
        var nextPosition = 1;

        for (var i = orderedPhases.Count - 1; i >= 0; i--)
        {
            var phase = orderedPhases[i];
            if (assignedTeams[phase.Id].Count == 0) continue;

            var phaseGames = gamesByPhase[phase.Id].ToList();
            var groups = groupsByPhase[phase.Id];

            var groupStandings = CalculateGroupStandings(groups, phaseGames, phase.Format, teamNameLookup);

            if (i == orderedPhases.Count - 1)
                AssignLastPhasePositions(groupStandings, teamNameLookup, levelName, result, positionedTeams, ref nextPosition);
            else
                AssignEarlierPhasePositions(phase, groupStandings, teamNameLookup, levelName, result, positionedTeams, ref nextPosition);
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
        List<(string GroupId, List<Standing> Standings)> groupStandings,
        Dictionary<string, string> teamNameLookup,
        string? levelName,
        List<FinalStanding> result,
        HashSet<string> positionedTeams,
        ref int nextPosition)
    {
        var ranked = RankCrossGroup(groupStandings);
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
        List<(string GroupId, List<Standing> Standings)> groupStandings,
        Dictionary<string, string> teamNameLookup,
        string? levelName,
        List<FinalStanding> result,
        HashSet<string> positionedTeams,
        ref int nextPosition)
    {
        var advancingTeamIds = GetAdvancingTeamIds(phase, groupStandings);
        var nonAdvancing = RankCrossGroup(groupStandings)
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

        var qualifyingIds = PhaseAdvancementCalculator.CalculateQualifyingTeamIds(phase, groupStandings);
        return qualifyingIds.ToHashSet();
    }

    private static List<Standing> RankCrossGroup(
        List<(string GroupId, List<Standing> Standings)> groupStandings)
    {
        return groupStandings
            .SelectMany(gs => gs.Standings)
            .OrderByDescending(s => s.Points ?? 0)
            .ThenByDescending(s => s.SetDifference ?? 0)
            .ThenByDescending(s => s.PointDifference ?? 0)
            .ThenByDescending(s => s.Wins)
            .ThenBy(s => s.Losses)
            .ToList();
    }
}
