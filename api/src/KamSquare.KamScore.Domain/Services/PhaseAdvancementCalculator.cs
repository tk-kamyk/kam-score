using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services;

public static class PhaseAdvancementCalculator
{
    /// <summary>
    /// Determines which teams qualify from a completed phase based on GroupWinners and TotalTeamsProceeding.
    /// Returns the qualifying team IDs (unordered).
    /// </summary>
    public static List<string> CalculateQualifyingTeamIds(
        Phase phase,
        List<(string GroupId, List<Standing> Standings)> groupStandings)
    {
        var groupWinners = phase.GroupWinners;
        var totalTeamsProceeding = phase.TotalTeamsProceeding;

        if (groupWinners is null && totalTeamsProceeding is null)
            return [];

        if (groupWinners is not null && totalTeamsProceeding is null)
        {
            // Only GroupWinners: take top N from each group
            return groupStandings
                .SelectMany(gs => gs.Standings
                    .Where(s => s.Position <= groupWinners.Value)
                    .Select(s => s.TeamId))
                .ToList();
        }

        if (groupWinners is null)
        {
            // Only TotalTeamsProceeding: rank all teams across groups, take top N
            var allStandings = groupStandings
                .SelectMany(gs => gs.Standings)
                .ToList();

            return RankCrossGroup(allStandings)
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

            var ranked = RankCrossGroup(remaining);
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
    /// Returns team IDs ordered from Seed 1 (best) to Seed N (worst).
    /// </summary>
    public static List<string> CalculateSeeding(
        List<string> qualifyingTeamIds,
        List<(string GroupId, List<Standing> Standings)> groupStandings)
    {
        var qualifyingSet = qualifyingTeamIds.ToHashSet();

        var qualifyingStandings = groupStandings
            .SelectMany(gs => gs.Standings)
            .Where(s => qualifyingSet.Contains(s.TeamId))
            .ToList();

        return RankCrossGroup(qualifyingStandings)
            .Select(s => s.TeamId)
            .ToList();
    }

    /// <summary>
    /// Determines the total number of teams that will advance from a phase.
    /// Used for placeholder game generation before the phase is complete.
    /// </summary>
    public static int? GetExpectedTeamCount(Phase phase)
    {
        if (phase.TotalTeamsProceeding is not null)
            return phase.TotalTeamsProceeding;

        if (phase.GroupWinners is not null)
            return phase.GroupWinners.Value * phase.Groups.Count;

        return null;
    }

    /// <summary>
    /// Ranks standings from multiple groups together using cross-group comparison criteria:
    /// points desc → set difference desc → point difference desc.
    /// No direct result tiebreaker across groups.
    /// </summary>
    private static List<Standing> RankCrossGroup(List<Standing> standings)
    {
        return standings
            .OrderByDescending(s => s.Points ?? 0)
            .ThenByDescending(s => s.SetDifference ?? 0)
            .ThenByDescending(s => s.PointDifference ?? 0)
            .ThenByDescending(s => s.Wins)
            .ThenBy(s => s.Losses)
            .ToList();
    }
}
