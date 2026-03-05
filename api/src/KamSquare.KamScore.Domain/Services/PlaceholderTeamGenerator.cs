using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class PlaceholderTeamGenerator
{
    /// <summary>
    /// Creates placeholder Team entities for a phase based on the source phase's progression config.
    /// Count = TotalTeamsProceeding ?? (GroupWinners × number of groups).
    /// Returns null if the source phase has no progression config.
    /// </summary>
    public static List<Team>? Generate(Phase sourcePhase, string tournamentId)
    {
        var count = PhaseAdvancementCalculator.GetExpectedTeamCount(sourcePhase);
        if (count is null)
            return null;

        return Enumerable.Range(1, count.Value)
            .Select(seed => Team.CreatePlaceholder(
                FormatPlaceholderName(sourcePhase.Name, seed),
                tournamentId,
                sourcePhase.Id,
                seed))
            .ToList();
    }

    public static string FormatPlaceholderName(string sourcePhaseName, int seed) =>
        $"{sourcePhaseName} - Seed {seed}";
}
