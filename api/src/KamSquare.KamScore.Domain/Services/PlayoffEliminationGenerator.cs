using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class PlayoffEliminationGenerator
{
    /// <summary>
    /// Generates single-elimination bracket games for a group.
    /// First round uses real team IDs (seeded). Later rounds use placeholders.
    /// Higher seeds get byes when team count is not a power of 2.
    /// </summary>
    public static List<Game> Generate(
        string tournamentId,
        string phaseId,
        string groupId,
        List<string> teamIds)
    {
        if (teamIds.Count <= 1)
            return [];

        if (teamIds.Count == 2)
        {
            return
            [
                Game.Create(tournamentId, phaseId, groupId, round: 1,
                    homeTeamId: teamIds[0], awayTeamId: teamIds[1],
                    homeTeamPlaceholder: null, awayTeamPlaceholder: null)
            ];
        }

        var games = new List<Game>();
        var n = teamIds.Count;
        var bracketSize = NextPowerOfTwo(n);
        var totalRounds = (int)Math.Log2(bracketSize);
        var byeCount = bracketSize - n;

        // Build seeded bracket positions for first round
        var bracketOrder = BuildBracketOrder(bracketSize);

        // Track game outcomes by bracket position for placeholder references
        // Key: (round, matchIndex) -> game
        var gameMap = new Dictionary<(int round, int matchIndex), Game>();
        var roundNames = GetRoundNames(totalRounds);

        // Generate first round
        var firstRoundMatchCount = bracketSize / 2;
        var gameIndex = 0;

        for (var match = 0; match < firstRoundMatchCount; match++)
        {
            var seed1Pos = bracketOrder[match * 2];
            var seed2Pos = bracketOrder[match * 2 + 1];

            var team1 = seed1Pos < n ? teamIds[seed1Pos] : null; // BYE
            var team2 = seed2Pos < n ? teamIds[seed2Pos] : null; // BYE

            if (team1 is null || team2 is null)
            {
                // One team has a bye - they auto-advance, no game needed
                var advancingTeamId = team1 ?? team2;

                // Create a phantom entry in gameMap for the next round to reference
                var phantomGame = Game.Create(tournamentId, phaseId, groupId, round: 1,
                    homeTeamId: advancingTeamId);
                gameMap[(1, match)] = phantomGame;
                continue;
            }

            gameIndex++;
            var matchLabel = GetMatchLabel(roundNames[0], gameIndex);
            var game = Game.Create(tournamentId, phaseId, groupId, round: 1,
                homeTeamId: team1, awayTeamId: team2);
            games.Add(game);
            gameMap[(1, match)] = game;
        }

        // Generate subsequent rounds
        for (var round = 2; round <= totalRounds; round++)
        {
            var matchesInRound = bracketSize / (int)Math.Pow(2, round);
            gameIndex = 0;

            for (var match = 0; match < matchesInRound; match++)
            {
                gameIndex++;
                var prevMatch1 = match * 2;
                var prevMatch2 = match * 2 + 1;

                var prevGame1 = gameMap.GetValueOrDefault((round - 1, prevMatch1));
                var prevGame2 = gameMap.GetValueOrDefault((round - 1, prevMatch2));

                string? homeTeamId = null;
                string? awayTeamId = null;
                string? homePlaceholder = null;
                string? awayPlaceholder = null;

                // If previous game was a bye (only has HomeTeamId, no AwayTeamId), team auto-advances
                if (prevGame1 is not null && prevGame1.AwayTeamId is null && prevGame1.HomeTeamId is not null)
                    homeTeamId = prevGame1.HomeTeamId;
                else
                    homePlaceholder = $"Winner {GetMatchLabel(roundNames[round - 2], prevMatch1 + 1)}";

                if (prevGame2 is not null && prevGame2.AwayTeamId is null && prevGame2.HomeTeamId is not null)
                    awayTeamId = prevGame2.HomeTeamId;
                else
                    awayPlaceholder = $"Winner {GetMatchLabel(roundNames[round - 2], prevMatch2 + 1)}";

                var game = Game.Create(tournamentId, phaseId, groupId, round: round,
                    homeTeamId: homeTeamId, awayTeamId: awayTeamId,
                    homeTeamPlaceholder: homePlaceholder, awayTeamPlaceholder: awayPlaceholder);
                games.Add(game);
                gameMap[(round, match)] = game;
            }
        }

        return games;
    }

    internal static int NextPowerOfTwo(int n)
    {
        var power = 1;
        while (power < n)
            power *= 2;
        return power;
    }

    /// <summary>
    /// Builds bracket ordering so that seed 1 plays seed N, seed 2 plays N-1, etc.
    /// Returns an array where positions[i] = seed index (0-based).
    /// </summary>
    internal static int[] BuildBracketOrder(int bracketSize)
    {
        // Start with [0, 1] and recursively build
        if (bracketSize == 2)
            return [0, 1];

        var halfOrder = BuildBracketOrder(bracketSize / 2);
        var order = new int[bracketSize];

        for (var i = 0; i < halfOrder.Length; i++)
        {
            order[i * 2] = halfOrder[i];
            order[i * 2 + 1] = bracketSize - 1 - halfOrder[i];
        }

        return order;
    }

    internal static string[] GetRoundNames(int totalRounds)
    {
        var names = new string[totalRounds];
        for (var i = 0; i < totalRounds; i++)
        {
            var roundFromEnd = totalRounds - i;
            names[i] = roundFromEnd switch
            {
                1 => "F",
                2 => "SF",
                3 => "QF",
                _ => $"R{i + 1}"
            };
        }
        return names;
    }

    internal static string GetMatchLabel(string roundName, int matchNumber)
    {
        if (roundName == "F")
            return "Final";
        return $"{roundName}{matchNumber}";
    }
}
