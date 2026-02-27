using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class PlayoffWithPlacementGenerator
{
    /// <summary>
    /// Generates single-elimination bracket with full placement games.
    /// Ordering: initial round → consolation (B) before main (A) at each level →
    /// placement games worst-to-best position → Final always last.
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
                    homeTeamId: teamIds[0], awayTeamId: teamIds[1])
            ];
        }

        var n = teamIds.Count;
        var bracketSize = PlayoffEliminationGenerator.NextPowerOfTwo(n);
        var totalMainRounds = (int)Math.Log2(bracketSize);
        var roundNames = PlayoffEliminationGenerator.GetRoundNames(totalMainRounds);

        // Phase 1: Generate first round (with byes)
        var (r1Games, r1Pool) = GenerateFirstRound(
            tournamentId, phaseId, groupId, teamIds, bracketSize, roundNames[0]);

        var eliminationGames = new List<Game>(r1Games);
        var placementGames = new List<Game>();
        var roundNumber = 1;

        // Phase 2: BFS through bracket tree
        var currentLevel = new List<(List<PoolEntry> Entries, string Prefix)>
        {
            (r1Pool, "")
        };

        while (currentLevel.Count > 0)
        {
            var nextLevel = new List<(List<PoolEntry> Entries, string Prefix)>();

            foreach (var (entries, prefix) in currentLevel)
            {
                // B-bracket: losers from this pool
                var loserEntries = entries.Where(e => e.HasLoser).ToList();
                if (loserEntries.Count >= 2)
                {
                    var bPrefix = prefix + "B-";
                    var bRoundName = GetSubBracketRoundName(loserEntries.Count / 2);
                    var bGames = new List<Game>();
                    var bPoolEntries = new List<PoolEntry>();

                    for (var i = 0; i < loserEntries.Count; i += 2)
                    {
                        var matchNum = i / 2 + 1;
                        var label = $"{bPrefix}{bRoundName}{matchNum}";
                        var game = Game.Create(tournamentId, phaseId, groupId,
                            round: 0, // temporary, assigned below
                            homeTeamPlaceholder: loserEntries[i].LoserRef,
                            awayTeamPlaceholder: loserEntries[i + 1].LoserRef);
                        bGames.Add(game);
                        bPoolEntries.Add(new PoolEntry(label, null));
                    }

                    if (loserEntries.Count == 2)
                    {
                        // Single game = placement (round assigned later)
                        placementGames.AddRange(bGames);
                    }
                    else
                    {
                        roundNumber++;
                        foreach (var g in bGames) g.Round = roundNumber;
                        eliminationGames.AddRange(bGames);
                        nextLevel.Add((bPoolEntries, bPrefix));
                    }
                }

                // A-bracket: winners from this pool
                if (entries.Count >= 2)
                {
                    var aPrefix = prefix + "A-";
                    var aRoundName = GetSubBracketRoundName(entries.Count / 2);
                    var aGames = new List<Game>();
                    var aPoolEntries = new List<PoolEntry>();

                    for (var i = 0; i < entries.Count; i += 2)
                    {
                        var matchNum = i / 2 + 1;
                        var label = $"{aPrefix}{aRoundName}{matchNum}";

                        // Determine home/away: use real team ID if bye, placeholder otherwise
                        string? homeTeamId = null, awayTeamId = null;
                        string? homePlaceholder = null, awayPlaceholder = null;

                        if (entries[i].IsBye)
                            homeTeamId = entries[i].WinnerRef;
                        else
                            homePlaceholder = entries[i].WinnerRef;

                        if (entries[i + 1].IsBye)
                            awayTeamId = entries[i + 1].WinnerRef;
                        else
                            awayPlaceholder = entries[i + 1].WinnerRef;

                        var game = Game.Create(tournamentId, phaseId, groupId,
                            round: 0,
                            homeTeamId: homeTeamId, awayTeamId: awayTeamId,
                            homeTeamPlaceholder: homePlaceholder, awayTeamPlaceholder: awayPlaceholder);
                        aGames.Add(game);
                        aPoolEntries.Add(new PoolEntry(label, null));
                    }

                    if (entries.Count == 2)
                    {
                        // Single game = placement (round assigned later)
                        placementGames.AddRange(aGames);
                    }
                    else
                    {
                        roundNumber++;
                        foreach (var g in aGames) g.Round = roundNumber;
                        eliminationGames.AddRange(aGames);
                        nextLevel.Add((aPoolEntries, aPrefix));
                    }
                }
            }

            currentLevel = nextLevel;
        }

        // Phase 3: Assign round numbers to placement games (already worst-to-best from BFS order)
        foreach (var game in placementGames)
        {
            roundNumber++;
            game.Round = roundNumber;
        }

        eliminationGames.AddRange(placementGames);
        return eliminationGames;
    }

    private static (List<Game> Games, List<PoolEntry> Pool) GenerateFirstRound(
        string tournamentId,
        string phaseId,
        string groupId,
        List<string> teamIds,
        int bracketSize,
        string roundName)
    {
        var n = teamIds.Count;
        var bracketOrder = PlayoffEliminationGenerator.BuildBracketOrder(bracketSize);
        var games = new List<Game>();
        var pool = new List<PoolEntry>();
        var gameIndex = 0;

        var firstRoundMatchCount = bracketSize / 2;

        for (var match = 0; match < firstRoundMatchCount; match++)
        {
            var seed1Pos = bracketOrder[match * 2];
            var seed2Pos = bracketOrder[match * 2 + 1];

            var team1 = seed1Pos < n ? teamIds[seed1Pos] : null;
            var team2 = seed2Pos < n ? teamIds[seed2Pos] : null;

            if (team1 is null || team2 is null)
            {
                // Bye: team auto-advances, no game
                var advancingTeamId = team1 ?? team2!;
                pool.Add(new PoolEntry($"BYE-{match}", advancingTeamId));
                continue;
            }

            gameIndex++;
            var label = PlayoffEliminationGenerator.GetMatchLabel(roundName, gameIndex);
            var game = Game.Create(tournamentId, phaseId, groupId, round: 1,
                homeTeamId: team1, awayTeamId: team2);
            games.Add(game);
            pool.Add(new PoolEntry(label, null));
        }

        return (games, pool);
    }

    private static string GetSubBracketRoundName(int gameCount)
    {
        return gameCount switch
        {
            1 => "F",
            2 => "SF",
            4 => "QF",
            _ => $"R{gameCount}"
        };
    }

    private record PoolEntry(string Label, string? AutoAdvanceTeamId)
    {
        public bool IsBye => AutoAdvanceTeamId is not null;
        public string WinnerRef => IsBye ? AutoAdvanceTeamId! : $"Winner {Label}";
        public bool HasLoser => !IsBye;
        public string LoserRef => $"Loser {Label}";
    }
}
