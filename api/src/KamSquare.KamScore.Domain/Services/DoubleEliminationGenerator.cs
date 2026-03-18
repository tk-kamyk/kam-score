using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class DoubleEliminationGenerator
{
    /// <summary>
    /// Generates a double-elimination bracket for a group.
    /// Winners Bracket (WB): standard single-elimination.
    /// Losers Bracket (LB): WB losers drop down; must lose twice to be eliminated.
    /// Grand Final: WB winner vs LB winner (no reset match).
    /// Round numbers are interleaved: WB-R1 → WB-R2 → LB-R1 → LB-R2 → WB-R3 → LB-R3 → LB-R4 → … → GF
    /// so that teams in both brackets get rest between games.
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
                    label: "Grand Final")
            ];
        }

        var n = teamIds.Count;
        var bracketSize = BracketUtilities.NextPowerOfTwo(n);
        var totalWbRounds = (int)Math.Log2(bracketSize);
        var wbRoundNames = BracketUtilities.GetRoundNames(totalWbRounds);

        var games = new List<Game>();
        var roundNumber = 0;
        var wbGameMap = new Dictionary<(int round, int matchIndex), WbEntry>();

        // === WB Round 1 (always first) ===
        roundNumber++;
        GenerateWbFirstRound(tournamentId, phaseId, groupId, teamIds, bracketSize, wbRoundNames,
            roundNumber, games, wbGameMap);

        // === WB Round 2 ===
        roundNumber++;
        GenerateWbRound(tournamentId, phaseId, groupId, bracketSize, wbRoundNames, totalWbRounds,
            2, roundNumber, games, wbGameMap);

        // === LB-R1: WB-R1 losers play each other ===
        var lbEntries = BuildInitialLbPool(wbGameMap, bracketSize);
        var lbRoundNumber = 0;

        if (lbEntries.Count >= 2)
        {
            lbRoundNumber++;
            roundNumber++;
            lbEntries = GenerateLbEliminationRound(tournamentId, phaseId, groupId, roundNumber,
                lbRoundNumber, lbEntries, games);
        }

        // === LB-R2: LB-R1 winners vs WB-R2 losers (drop-down) ===
        var wbR2Losers = GetWbLosers(wbGameMap, 2, bracketSize);
        if (wbR2Losers.Count > 0 && lbEntries.Count > 0)
        {
            lbRoundNumber++;
            roundNumber++;
            lbEntries = GenerateLbDropdownRound(tournamentId, phaseId, groupId, roundNumber,
                lbRoundNumber, lbEntries, wbR2Losers, games);
        }

        // === Interleaved: for each remaining WB round (3+) ===
        for (var wbRound = 3; wbRound <= totalWbRounds; wbRound++)
        {
            // LB elimination round (LB survivors play each other)
            if (lbEntries.Count >= 2)
            {
                lbRoundNumber++;
                roundNumber++;
                lbEntries = GenerateLbEliminationRound(tournamentId, phaseId, groupId, roundNumber,
                    lbRoundNumber, lbEntries, games);
            }

            // WB round
            roundNumber++;
            GenerateWbRound(tournamentId, phaseId, groupId, bracketSize, wbRoundNames, totalWbRounds,
                wbRound, roundNumber, games, wbGameMap);

            // LB dropdown round (LB survivors vs new WB losers)
            var wbLosers = GetWbLosers(wbGameMap, wbRound, bracketSize);
            if (wbLosers.Count > 0 && lbEntries.Count > 0)
            {
                lbRoundNumber++;
                roundNumber++;
                lbEntries = GenerateLbDropdownRound(tournamentId, phaseId, groupId, roundNumber,
                    lbRoundNumber, lbEntries, wbLosers, games);
            }
        }

        // === Grand Final ===
        roundNumber++;
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

        return games;
    }

    private static void GenerateWbFirstRound(
        string tournamentId, string phaseId, string groupId,
        List<string> teamIds, int bracketSize, string[] wbRoundNames,
        int roundNumber, List<Game> games,
        Dictionary<(int round, int matchIndex), WbEntry> wbGameMap)
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
                wbGameMap[(1, match)] = new WbEntry(null, advancingTeamId);
                continue;
            }

            gameIndex++;
            var label = $"WB-{BracketUtilities.GetMatchLabel(wbRoundNames[0], gameIndex)}";
            var game = Game.Create(tournamentId, phaseId, groupId, round: roundNumber,
                homeTeamId: team1, awayTeamId: team2, label: label);
            games.Add(game);
            wbGameMap[(1, match)] = new WbEntry(label, null);
        }
    }

    private static void GenerateWbRound(
        string tournamentId, string phaseId, string groupId,
        int bracketSize, string[] wbRoundNames, int totalWbRounds,
        int wbRound, int roundNumber, List<Game> games,
        Dictionary<(int round, int matchIndex), WbEntry> wbGameMap)
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
            wbGameMap[(wbRound, match)] = new WbEntry(label, null);
        }
    }

    private static void ResolveWbSlot(WbEntry? entry, ref string? teamId, ref string? placeholder)
    {
        if (entry is null) return;

        if (entry.ByeTeamId is not null)
            teamId = entry.ByeTeamId;
        else
            placeholder = $"Winner {entry.Label}";
    }

    private static List<LbEntry> GenerateLbEliminationRound(
        string tournamentId, string phaseId, string groupId,
        int roundNumber, int lbRoundNumber,
        List<LbEntry> lbEntries, List<Game> games)
    {
        var newEntries = new List<LbEntry>();

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
            newEntries.Add(new LbEntry(label, null));
        }

        return newEntries;
    }

    private static List<LbEntry> GenerateLbDropdownRound(
        string tournamentId, string phaseId, string groupId,
        int roundNumber, int lbRoundNumber,
        List<LbEntry> lbEntries, List<LbEntry> wbLosers, List<Game> games)
    {
        var newEntries = new List<LbEntry>();

        // Reverse dropdowns to avoid same-side matchups
        var dropdowns = new List<LbEntry>(wbLosers);
        dropdowns.Reverse();

        var maxPairs = Math.Min(lbEntries.Count, dropdowns.Count);
        for (var i = 0; i < maxPairs; i++)
        {
            var matchNum = i + 1;
            var label = $"LB-R{lbRoundNumber}-{matchNum}";

            var game = CreateLbGame(tournamentId, phaseId, groupId, roundNumber,
                lbEntries[i], dropdowns[i], label);
            games.Add(game);
            newEntries.Add(new LbEntry(label, null));
        }

        return newEntries;
    }

    private static List<LbEntry> BuildInitialLbPool(
        Dictionary<(int round, int matchIndex), WbEntry> wbGameMap,
        int bracketSize)
    {
        var entries = new List<LbEntry>();
        var firstRoundMatchCount = bracketSize / 2;

        for (var match = 0; match < firstRoundMatchCount; match++)
        {
            var entry = wbGameMap.GetValueOrDefault((1, match));
            if (entry is null) continue;

            if (entry.Label is not null)
            {
                entries.Add(new LbEntry($"Loser {entry.Label}", null));
            }
        }

        return entries;
    }

    private static List<LbEntry> GetWbLosers(
        Dictionary<(int round, int matchIndex), WbEntry> wbGameMap,
        int wbRound,
        int bracketSize)
    {
        var losers = new List<LbEntry>();
        var matchesInRound = bracketSize / (int)Math.Pow(2, wbRound);

        for (var match = 0; match < matchesInRound; match++)
        {
            var entry = wbGameMap.GetValueOrDefault((wbRound, match));
            if (entry?.Label is not null)
            {
                losers.Add(new LbEntry($"Loser {entry.Label}", null));
            }
        }

        return losers;
    }

    private static Game CreateLbGame(
        string tournamentId, string phaseId, string groupId, int round,
        LbEntry home, LbEntry away, string label)
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

    private record WbEntry(string? Label, string? ByeTeamId);
    private record LbEntry(string? Label, string? ByeTeamId);
}
