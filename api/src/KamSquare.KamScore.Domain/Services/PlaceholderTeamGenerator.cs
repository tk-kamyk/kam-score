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

        if (sourcePhase.Levels.Count == 0)
            return Enumerable.Range(1, count.Value)
                .Select(seed => Team.CreatePlaceholder(
                    FormatPlaceholderName(sourcePhase.Name, seed),
                    tournamentId,
                    sourcePhase.Id,
                    seed))
                .ToList();

        var teamsPerLevel = count.Value / sourcePhase.Levels.Count;
        var result = new List<Team>();
        var globalSeed = 1;

        foreach (var level in sourcePhase.Levels.OrderBy(l => l.Order))
        {
            for (var levelSeed = 1; levelSeed <= teamsPerLevel; levelSeed++, globalSeed++)
            {
                result.Add(Team.CreatePlaceholder(
                    FormatPlaceholderName(sourcePhase.Name, level.Name, levelSeed),
                    tournamentId,
                    sourcePhase.Id,
                    globalSeed));
            }
        }

        return result;
    }

    public static string FormatPlaceholderName(string sourcePhaseName, int seed) =>
        $"{sourcePhaseName} - Seed {seed}";

    public static string FormatPlaceholderName(string sourcePhaseName, string levelName, int levelSeed) =>
        $"{sourcePhaseName} - {levelName} - Seed {levelSeed}";
}
