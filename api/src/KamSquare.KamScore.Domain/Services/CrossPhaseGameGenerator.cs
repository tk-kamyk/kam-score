using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Domain.Services;

public static class CrossPhaseGameGenerator
{
    private const string SeedPrefix = "__seed_";
    private const string SeedSuffix = "__";

    /// <summary>
    /// Generates games for a phase using cross-phase placeholders instead of real team IDs.
    /// Uses existing generators internally with virtual team IDs, then post-processes to convert
    /// them into placeholder strings.
    /// </summary>
    public static List<Game> GenerateWithPlaceholders(
        string tournamentId,
        string phaseId,
        Phase phase,
        string sourcePhaseName,
        int totalTeams)
    {
        // Create virtual seed IDs and distribute to groups via snake draft
        var seedIds = Enumerable.Range(1, totalTeams)
            .Select(i => $"{SeedPrefix}{i}{SeedSuffix}")
            .ToList();

        var groupTeams = DistributeToGroups(seedIds, phase.Groups.Count);

        // Generate games per group using existing generators with virtual IDs
        var allGames = new List<Game>();
        for (var i = 0; i < phase.Groups.Count; i++)
        {
            var group = phase.Groups[i];
            var teamIds = groupTeams[i];

            if (teamIds.Count <= 1) continue;

            var games = phase.Format switch
            {
                PhaseFormat.RoundRobin => RoundRobinGenerator.Generate(
                    tournamentId, phaseId, group.Id, teamIds),
                PhaseFormat.PlayoffElimination => PlayoffEliminationGenerator.Generate(
                    tournamentId, phaseId, group.Id, teamIds),
                PhaseFormat.PlayoffWithPlacement => PlayoffWithPlacementGenerator.Generate(
                    tournamentId, phaseId, group.Id, teamIds),
                _ => []
            };

            allGames.AddRange(games);
        }

        // Post-process: convert virtual seed IDs to cross-phase placeholders
        ConvertToPlaceholders(allGames, sourcePhaseName);

        return allGames;
    }

    /// <summary>
    /// Distributes seed positions across groups using snake draft ordering.
    /// Same algorithm as TournamentStructure.AutoAssignTeams.
    /// </summary>
    private static List<List<string>> DistributeToGroups(List<string> seedIds, int groupCount)
    {
        var groups = Enumerable.Range(0, groupCount)
            .Select(_ => new List<string>())
            .ToList();

        if (groupCount == 0) return groups;

        for (var i = 0; i < seedIds.Count; i++)
        {
            var round = i / groupCount;
            var positionInRound = i % groupCount;
            var groupIndex = round % 2 == 0 ? positionInRound : groupCount - 1 - positionInRound;
            groups[groupIndex].Add(seedIds[i]);
        }

        return groups;
    }

    /// <summary>
    /// Converts virtual seed IDs in games to cross-phase placeholder strings.
    /// Virtual IDs in HomeTeamId/AwayTeamId are moved to HomeTeamPlaceholder/AwayTeamPlaceholder.
    /// Within-phase placeholders (e.g., "Winner SF1") are left unchanged.
    /// </summary>
    private static void ConvertToPlaceholders(List<Game> games, string sourcePhaseName)
    {
        foreach (var game in games)
        {
            if (IsSeedId(game.HomeTeamId))
            {
                var seedNumber = ExtractSeedNumber(game.HomeTeamId!);
                game.HomeTeamPlaceholder = FormatPlaceholder(sourcePhaseName, seedNumber);
                game.HomeTeamId = null;
            }

            if (IsSeedId(game.AwayTeamId))
            {
                var seedNumber = ExtractSeedNumber(game.AwayTeamId!);
                game.AwayTeamPlaceholder = FormatPlaceholder(sourcePhaseName, seedNumber);
                game.AwayTeamId = null;
            }

            // Clear referee for placeholder games (no real teams to referee)
            if (IsSeedId(game.RefereeTeamId))
            {
                game.RefereeTeamId = null;
            }
        }
    }

    private static bool IsSeedId(string? id) =>
        id is not null && id.StartsWith(SeedPrefix) && id.EndsWith(SeedSuffix);

    private static int ExtractSeedNumber(string seedId) =>
        int.Parse(seedId[SeedPrefix.Length..^SeedSuffix.Length]);

    public static string FormatPlaceholder(string sourcePhaseName, int seedNumber) =>
        $"{sourcePhaseName} - Seed {seedNumber}";
}
