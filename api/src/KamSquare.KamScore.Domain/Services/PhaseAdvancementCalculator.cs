using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services.Formats;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services;

public static class PhaseAdvancementCalculator
{
    /// <summary>
    /// Determines which teams qualify from a completed phase based on GroupWinners and TotalTeamsProceeding.
    /// When levels exist, progression is applied per-level and results are concatenated (Level 1 first).
    /// Returns the qualifying team IDs.
    /// </summary>
    public static List<string> CalculateQualifyingTeamIds(
        Phase phase,
        List<(string GroupId, List<Standing> Standings)> groupStandings)
    {
        if (phase.GroupWinners is null && phase.TotalTeamsProceeding is null)
            return [];

        if (phase.Levels.Count == 0)
            return QualifyFromStandings(phase.Format, phase.GroupWinners, phase.TotalTeamsProceeding, groupStandings);

        var result = new List<string>();
        foreach (var level in phase.Levels.OrderBy(l => l.Order))
        {
            var levelGroupIds = phase.Groups.Where(g => g.LevelId == level.Id).Select(g => g.Id).ToHashSet();
            var levelStandings = groupStandings.Where(gs => levelGroupIds.Contains(gs.GroupId)).ToList();
            result.AddRange(QualifyFromStandings(phase.Format, phase.GroupWinners, phase.TotalTeamsProceeding, levelStandings));
        }

        return result;
    }

    private static List<string> QualifyFromStandings(
        PhaseFormat format,
        int? groupWinners,
        int? totalTeamsProceeding,
        List<(string GroupId, List<Standing> Standings)> groupStandings)
    {
        if (groupWinners is not null && totalTeamsProceeding is null)
        {
            return groupStandings
                .SelectMany(gs => gs.Standings
                    .Where(s => s.Position <= groupWinners.Value)
                    .Select(s => s.TeamId))
                .ToList();
        }

        if (groupWinners is null)
        {
            var allStandings = groupStandings
                .SelectMany(gs => gs.Standings)
                .ToList();

            return PhaseFormatStrategy.For(format).RankCrossGroup(allStandings)
                .Take(totalTeamsProceeding!.Value)
                .Select(s => s.TeamId)
                .ToList();
        }

        // Both set: group winners qualify first, then best remaining fill up to total
        var qualifyingIds = new HashSet<string>();

        foreach (var (_, standings) in groupStandings)
        {
            foreach (var standing in standings.Where(s => s.Position <= groupWinners.Value))
            {
                qualifyingIds.Add(standing.TeamId);
            }
        }

        if (qualifyingIds.Count < totalTeamsProceeding!.Value)
        {
            var remaining = groupStandings
                .SelectMany(gs => gs.Standings)
                .Where(s => !qualifyingIds.Contains(s.TeamId))
                .ToList();

            var ranked = PhaseFormatStrategy.For(format).RankCrossGroup(remaining);
            var slotsToFill = totalTeamsProceeding.Value - qualifyingIds.Count;

            foreach (var standing in ranked.Take(slotsToFill))
            {
                qualifyingIds.Add(standing.TeamId);
            }
        }

        return qualifyingIds.ToList();
    }

    /// <summary>
    /// Ranks all qualifying teams together in a single seeding order.
    /// When levels exist, teams are ranked within each level and concatenated (Level 1 first).
    /// Returns team IDs ordered from Seed 1 (best) to Seed N (worst).
    /// </summary>
    public static List<string> CalculateSeeding(
        List<string> qualifyingTeamIds,
        List<(string GroupId, List<Standing> Standings)> groupStandings,
        Phase phase)
    {
        var qualifyingSet = qualifyingTeamIds.ToHashSet();

        if (phase.Levels.Count == 0)
        {
            var qualifyingStandings = groupStandings
                .SelectMany(gs => gs.Standings)
                .Where(s => qualifyingSet.Contains(s.TeamId))
                .ToList();

            return PhaseFormatStrategy.For(phase.Format).RankCrossGroup(qualifyingStandings)
                .Select(s => s.TeamId)
                .ToList();
        }

        var result = new List<string>();
        foreach (var level in phase.Levels.OrderBy(l => l.Order))
        {
            var levelGroupIds = phase.Groups.Where(g => g.LevelId == level.Id).Select(g => g.Id).ToHashSet();
            var levelStandings = groupStandings
                .Where(gs => levelGroupIds.Contains(gs.GroupId))
                .SelectMany(gs => gs.Standings)
                .Where(s => qualifyingSet.Contains(s.TeamId))
                .ToList();

            result.AddRange(PhaseFormatStrategy.For(phase.Format).RankCrossGroup(levelStandings).Select(s => s.TeamId));
        }

        return result;
    }

    /// <summary>
    /// Determines the total number of teams that will advance from a phase.
    /// When levels exist, TotalTeamsProceeding is per-level and multiplied by level count.
    /// GroupWinners × Groups.Count already accounts for levels since Groups.Count includes all level groups.
    /// </summary>
    public static int? GetExpectedTeamCount(Phase phase)
    {
        if (phase.TotalTeamsProceeding is not null)
        {
            var levelMultiplier = phase.Levels.Count > 0 ? phase.Levels.Count : 1;
            return phase.TotalTeamsProceeding.Value * levelMultiplier;
        }

        if (phase.GroupWinners is not null)
            return phase.GroupWinners.Value * phase.Groups.Count;

        return null;
    }
}
