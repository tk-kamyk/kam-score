using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services.Formats;

/// <summary>
/// Generates a standard double-elimination bracket (WB + LB + Grand Final).
/// See docs/design/game-generation.md.
/// </summary>
public static class DoubleEliminationGenerator
{
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
                    label: "Grand Final")
            ];
        }

        var n = teamIds.Count;
        var bracketSize = BracketUtilities.NextPowerOfTwo(n);
        var totalWbRounds = (int)Math.Log2(bracketSize);
        var wbRoundNames = BracketUtilities.GetRoundNames(totalWbRounds);

        var games = new List<Game>();
        var roundNumber = 0;
        var wbGameMap = new Dictionary<(int round, int matchIndex), WinnerBracketSlot>();

        // WB Round 1
        roundNumber++;
        GenerateWbFirstRound(tournamentId, phaseId, groupId, teamIds, bracketSize, wbRoundNames,
            roundNumber, games, wbGameMap);

        // WB Round 2
        roundNumber++;
        GenerateWbRound(tournamentId, phaseId, groupId, bracketSize, wbRoundNames, totalWbRounds,
            2, roundNumber, games, wbGameMap);

        var lbEntries = BuildInitialLbPool(wbGameMap, bracketSize);
        var lbRoundNumber = 0;

        GenerateInitialLoserBracketRounds(tournamentId, phaseId, groupId, bracketSize,
            wbGameMap, lbEntries, games, ref roundNumber, ref lbRoundNumber);

        GenerateInterleavedRounds(tournamentId, phaseId, groupId, bracketSize,
            wbRoundNames, totalWbRounds, wbGameMap, lbEntries, games, ref roundNumber, ref lbRoundNumber);

        roundNumber++;
        GenerateGrandFinal(tournamentId, phaseId, groupId, roundNumber,
            wbGameMap, totalWbRounds, lbEntries, games);

        return games;
    }

    private static void GenerateWbFirstRound(
        string tournamentId, string phaseId, string groupId,
        List<string> teamIds, int bracketSize, string[] wbRoundNames,
        int roundNumber, List<Game> games,
        Dictionary<(int round, int matchIndex), WinnerBracketSlot> wbGameMap)
    {
        var n = teamIds.Count;
        var bracketOrder = BracketUtilities.BuildBracketOrder(bracketSize);
        var firstRoundMatchCount = bracketSize / 2;
        var gameIndex = 0;

        for (var match = 0; match < firstRoundMatchCount; match++)
        {
            var seed1Pos = bracketOrder[match * 2];
            var seed2Pos = bracketOrder[match * 2 + 1];

            var team1 = seed1Pos < n ? teamIds[seed1Pos] : null;
            var team2 = seed2Pos < n ? teamIds[seed2Pos] : null;

            if (team1 is null || team2 is null)
            {
                var advancingTeamId = team1 ?? team2!;
                wbGameMap[(1, match)] = new WinnerBracketSlot(null, advancingTeamId);
                continue;
            }

            gameIndex++;
            var label = $"WB-{BracketUtilities.GetMatchLabel(wbRoundNames[0], gameIndex)}";
            var game = Game.Create(tournamentId, phaseId, groupId, round: roundNumber,
                homeTeamId: team1, awayTeamId: team2, label: label);
            games.Add(game);
            wbGameMap[(1, match)] = new WinnerBracketSlot(label, null);
        }
    }

    private static void GenerateWbRound(
        string tournamentId, string phaseId, string groupId,
        int bracketSize, string[] wbRoundNames, int totalWbRounds,
        int wbRound, int roundNumber, List<Game> games,
        Dictionary<(int round, int matchIndex), WinnerBracketSlot> wbGameMap)
    {
        var matchesInRound = bracketSize / (int)Math.Pow(2, wbRound);
        var gameIndex = 0;

        for (var match = 0; match < matchesInRound; match++)
        {
            gameIndex++;
            var prev1 = wbGameMap.GetValueOrDefault((wbRound - 1, match * 2));
            var prev2 = wbGameMap.GetValueOrDefault((wbRound - 1, match * 2 + 1));

            string? homeTeamId = null, awayTeamId = null;
            string? homePlaceholder = null, awayPlaceholder = null;

            ResolveWbSlot(prev1, ref homeTeamId, ref homePlaceholder);
            ResolveWbSlot(prev2, ref awayTeamId, ref awayPlaceholder);

            var roundName = wbRound == totalWbRounds ? "F" : wbRoundNames[wbRound - 1];
            var label = $"WB-{BracketUtilities.GetMatchLabel(roundName, gameIndex)}";
            var game = Game.Create(tournamentId, phaseId, groupId, round: roundNumber,
                homeTeamId: homeTeamId, awayTeamId: awayTeamId,
                homeTeamPlaceholder: homePlaceholder, awayTeamPlaceholder: awayPlaceholder,
                label: label);
            games.Add(game);
            wbGameMap[(wbRound, match)] = new WinnerBracketSlot(label, null);
        }
    }

    private static void ResolveWbSlot(WinnerBracketSlot? entry, ref string? teamId, ref string? placeholder)
    {
        if (entry is null) return;

        if (entry.ByeTeamId is not null)
            teamId = entry.ByeTeamId;
        else
            placeholder = $"Winner {entry.Label}";
    }

    private static void GenerateInitialLoserBracketRounds(
        string tournamentId, string phaseId, string groupId, int bracketSize,
        Dictionary<(int round, int matchIndex), WinnerBracketSlot> wbGameMap,
        List<LoserBracketSlot> lbEntries, List<Game> games,
        ref int roundNumber, ref int lbRoundNumber)
    {
        if (lbEntries.Count >= 2)
        {
            lbRoundNumber++;
            roundNumber++;
            var updated = GenerateLbEliminationRound(tournamentId, phaseId, groupId, roundNumber,
                lbRoundNumber, lbEntries, games);
            lbEntries.Clear();
            lbEntries.AddRange(updated);
        }

        var wbR2Losers = GetWbLosers(wbGameMap, 2, bracketSize);
        if (wbR2Losers.Count > 0 && lbEntries.Count > 0)
        {
            lbRoundNumber++;
            roundNumber++;
            var updated = GenerateLbDropdownRound(tournamentId, phaseId, groupId, roundNumber,
                lbRoundNumber, lbEntries, wbR2Losers, games);
            lbEntries.Clear();
            lbEntries.AddRange(updated);
        }
    }

    private static void GenerateInterleavedRounds(
        string tournamentId, string phaseId, string groupId, int bracketSize,
        string[] wbRoundNames, int totalWbRounds,
        Dictionary<(int round, int matchIndex), WinnerBracketSlot> wbGameMap,
        List<LoserBracketSlot> lbEntries, List<Game> games,
        ref int roundNumber, ref int lbRoundNumber)
    {
        for (var wbRound = 3; wbRound <= totalWbRounds; wbRound++)
        {
            if (lbEntries.Count >= 2)
            {
                lbRoundNumber++;
                roundNumber++;
                var updated = GenerateLbEliminationRound(tournamentId, phaseId, groupId, roundNumber,
                    lbRoundNumber, lbEntries, games);
                lbEntries.Clear();
                lbEntries.AddRange(updated);
            }

            roundNumber++;
            GenerateWbRound(tournamentId, phaseId, groupId, bracketSize, wbRoundNames, totalWbRounds,
                wbRound, roundNumber, games, wbGameMap);

            var wbLosers = GetWbLosers(wbGameMap, wbRound, bracketSize);
            if (wbLosers.Count > 0 && lbEntries.Count > 0)
            {
                lbRoundNumber++;
                roundNumber++;
                var updated = GenerateLbDropdownRound(tournamentId, phaseId, groupId, roundNumber,
                    lbRoundNumber, lbEntries, wbLosers, games);
                lbEntries.Clear();
                lbEntries.AddRange(updated);
            }
        }
    }

    private static void GenerateGrandFinal(
        string tournamentId, string phaseId, string groupId, int roundNumber,
        Dictionary<(int round, int matchIndex), WinnerBracketSlot> wbGameMap,
        int totalWbRounds, List<LoserBracketSlot> lbEntries, List<Game> games)
    {
        var wbWinnerLabel = wbGameMap[(totalWbRounds, 0)].Label!;
        string? gfHomeId = null, gfAwayId = null;
        string? gfHomePlaceholder = $"Winner {wbWinnerLabel}";
        string? gfAwayPlaceholder = lbEntries.Count == 1
            ? (lbEntries[0].ByeTeamId ?? $"Winner {lbEntries[0].Label}")
            : null;

        if (lbEntries.Count == 1 && lbEntries[0].ByeTeamId is not null)
        {
            gfAwayId = lbEntries[0].ByeTeamId;
            gfAwayPlaceholder = null;
        }

        var grandFinal = Game.Create(tournamentId, phaseId, groupId, round: roundNumber,
            homeTeamId: gfHomeId, awayTeamId: gfAwayId,
            homeTeamPlaceholder: gfHomePlaceholder, awayTeamPlaceholder: gfAwayPlaceholder,
            label: "Grand Final");
        games.Add(grandFinal);
    }

    private static List<LoserBracketSlot> GenerateLbEliminationRound(
        string tournamentId, string phaseId, string groupId,
        int roundNumber, int lbRoundNumber,
        List<LoserBracketSlot> lbEntries, List<Game> games)
    {
        var newEntries = new List<LoserBracketSlot>();

        for (var i = 0; i < lbEntries.Count; i += 2)
        {
            if (i + 1 >= lbEntries.Count)
            {
                newEntries.Add(lbEntries[i]);
                continue;
            }

            var matchNum = i / 2 + 1;
            var label = $"LB-R{lbRoundNumber}-{matchNum}";

            var game = CreateLbGame(tournamentId, phaseId, groupId, roundNumber,
                lbEntries[i], lbEntries[i + 1], label);
            games.Add(game);
            newEntries.Add(new LoserBracketSlot(label, null));
        }

        return newEntries;
    }

    private static List<LoserBracketSlot> GenerateLbDropdownRound(
        string tournamentId, string phaseId, string groupId,
        int roundNumber, int lbRoundNumber,
        List<LoserBracketSlot> lbEntries, List<LoserBracketSlot> wbLosers, List<Game> games)
    {
        var newEntries = new List<LoserBracketSlot>();

        var dropdowns = new List<LoserBracketSlot>(wbLosers);
        dropdowns.Reverse();

        var maxPairs = Math.Min(lbEntries.Count, dropdowns.Count);
        for (var i = 0; i < maxPairs; i++)
        {
            var matchNum = i + 1;
            var label = $"LB-R{lbRoundNumber}-{matchNum}";

            var game = CreateLbGame(tournamentId, phaseId, groupId, roundNumber,
                lbEntries[i], dropdowns[i], label);
            games.Add(game);
            newEntries.Add(new LoserBracketSlot(label, null));
        }

        return newEntries;
    }

    private static List<LoserBracketSlot> BuildInitialLbPool(
        Dictionary<(int round, int matchIndex), WinnerBracketSlot> wbGameMap,
        int bracketSize)
    {
        var entries = new List<LoserBracketSlot>();
        var firstRoundMatchCount = bracketSize / 2;

        for (var match = 0; match < firstRoundMatchCount; match++)
        {
            var entry = wbGameMap.GetValueOrDefault((1, match));
            if (entry is null) continue;

            if (entry.Label is not null)
            {
                entries.Add(new LoserBracketSlot($"Loser {entry.Label}", null));
            }
        }

        return entries;
    }

    private static List<LoserBracketSlot> GetWbLosers(
        Dictionary<(int round, int matchIndex), WinnerBracketSlot> wbGameMap,
        int wbRound,
        int bracketSize)
    {
        var losers = new List<LoserBracketSlot>();
        var matchesInRound = bracketSize / (int)Math.Pow(2, wbRound);

        for (var match = 0; match < matchesInRound; match++)
        {
            var entry = wbGameMap.GetValueOrDefault((wbRound, match));
            if (entry?.Label is not null)
            {
                losers.Add(new LoserBracketSlot($"Loser {entry.Label}", null));
            }
        }

        return losers;
    }

    private static Game CreateLbGame(
        string tournamentId, string phaseId, string groupId, int round,
        LoserBracketSlot home, LoserBracketSlot away, string label)
    {
        string? homeTeamId = null, awayTeamId = null;
        string? homePlaceholder = null, awayPlaceholder = null;

        if (home.ByeTeamId is not null)
            homeTeamId = home.ByeTeamId;
        else
            homePlaceholder = home.Label!.StartsWith("Loser ") ? home.Label : $"Winner {home.Label}";

        if (away.ByeTeamId is not null)
            awayTeamId = away.ByeTeamId;
        else
            awayPlaceholder = away.Label!.StartsWith("Loser ") ? away.Label : $"Winner {away.Label}";

        return Game.Create(tournamentId, phaseId, groupId, round: round,
            homeTeamId: homeTeamId, awayTeamId: awayTeamId,
            homeTeamPlaceholder: homePlaceholder, awayTeamPlaceholder: awayPlaceholder,
            label: label);
    }

    private record WinnerBracketSlot(string? Label, string? ByeTeamId);
    private record LoserBracketSlot(string? Label, string? ByeTeamId);
}
