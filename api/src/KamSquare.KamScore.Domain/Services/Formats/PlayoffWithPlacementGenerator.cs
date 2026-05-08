using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services.Formats;

/// <summary>
/// Generates a playoff bracket with full placement games so every team ends
/// up with a unique final position. See docs/design/game-generation.md for
/// the worst-to-best placement-game ordering.
/// </summary>
public static class PlayoffWithPlacementGenerator
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
                    label: "Final")
            ];
        }

        var bracketSize = BracketUtilities.NextPowerOfTwo(teamIds.Count);
        var totalMainRounds = (int)Math.Log2(bracketSize);
        var roundNames = BracketUtilities.GetRoundNames(totalMainRounds);

        var (firstRoundGames, firstRoundSlots) = BracketUtilities.BuildFirstRoundPool(
            tournamentId, phaseId, groupId, teamIds, bracketSize, roundNames[0]);

        var firstRoundPool = firstRoundSlots.Select(WrapSlot).ToList();

        // Bye-last reorder: pair groupings sort so 0-bye pairs come first,
        // 2-bye pairs next, and 1-bye mixed pairs last. Pushes the bye seed's
        // pair to the last A-SF slot.
        firstRoundPool = BracketUtilities.ReorderPairsByByeLast(firstRoundPool, e => e is PoolEntry.FirstRoundBye);

        var eliminationGames = new List<Game>(firstRoundGames);
        var placementGames = new List<Game>();
        var roundNumber = 1;

        var currentLevel = new List<(List<PoolEntry> Entries, string Prefix)>
        {
            (firstRoundPool, "")
        };

        while (currentLevel.Count > 0)
        {
            var nextLevel = new List<(List<PoolEntry> Entries, string Prefix)>();

            foreach (var (entries, prefix) in currentLevel)
            {
                ProcessLoserSubBracket(tournamentId, phaseId, groupId, entries, prefix,
                    eliminationGames, placementGames, nextLevel, ref roundNumber);

                ProcessWinnerSubBracket(tournamentId, phaseId, groupId, entries, prefix,
                    eliminationGames, placementGames, nextLevel, ref roundNumber);
            }

            currentLevel = nextLevel;
        }

        foreach (var game in placementGames)
        {
            roundNumber++;
            game.Round = roundNumber;
        }

        eliminationGames.AddRange(placementGames);
        return eliminationGames;
    }

    private static PoolEntry WrapSlot(BracketUtilities.FirstRoundSlot slot) =>
        slot switch
        {
            BracketUtilities.FirstRoundSlot.Bye b => new PoolEntry.FirstRoundBye(b.TeamId),
            BracketUtilities.FirstRoundSlot.Real r => new PoolEntry.Game(r.GameLabel),
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown FirstRoundSlot kind"),
        };

    private static void ProcessLoserSubBracket(
        string tournamentId, string phaseId, string groupId,
        List<PoolEntry> entries, string prefix,
        List<Game> eliminationGames, List<Game> placementGames,
        List<(List<PoolEntry> Entries, string Prefix)> nextLevel,
        ref int roundNumber)
    {
        var loserEntries = entries.OfType<PoolEntry.Game>().Cast<PoolEntry>().ToList();
        if (loserEntries.Count < 2)
            return;

        // Why: When the loser pool size is odd, append a synthetic loser-bye
        // after the last real loser. The pair (last real loser, loser-bye) does
        // not produce a game; the real loser auto-advances to the next level
        // via a placeholder pool entry.
        if (loserEntries.Count % 2 == 1)
        {
            var lastReal = (PoolEntry.Game)loserEntries[^1];
            loserEntries.Add(new PoolEntry.LoserBye(lastReal.LoserRef));
        }

        var bPrefix = prefix + "B-";
        var bRoundName = GetSubBracketRoundName(loserEntries.Count / 2);
        var bGames = new List<Game>();
        var bPoolEntries = new List<PoolEntry>();
        var matchNum = 0;

        for (var i = 0; i < loserEntries.Count; i += 2)
        {
            var first = loserEntries[i];
            var second = loserEntries[i + 1];

            // Auto-advance: when one side of the pair is a synthetic loser-bye,
            // the partnered real loser propagates as a placeholder pool entry.
            if (first is not PoolEntry.Game firstGame || second is not PoolEntry.Game secondGame)
            {
                var realLoser = (first is PoolEntry.Game fg) ? fg : (PoolEntry.Game)second;
                bPoolEntries.Add(new PoolEntry.LoserBye(realLoser.LoserRef));
                continue;
            }

            matchNum++;
            var label = $"{bPrefix}{bRoundName}{matchNum}";
            var game = Game.Create(tournamentId, phaseId, groupId,
                round: 0,
                homeTeamPlaceholder: firstGame.LoserRef,
                awayTeamPlaceholder: secondGame.LoserRef,
                label: label);
            bGames.Add(game);
            bPoolEntries.Add(new PoolEntry.Game(label));
        }

        if (loserEntries.Count == 2)
        {
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

    private static void ProcessWinnerSubBracket(
        string tournamentId, string phaseId, string groupId,
        List<PoolEntry> entries, string prefix,
        List<Game> eliminationGames, List<Game> placementGames,
        List<(List<PoolEntry> Entries, string Prefix)> nextLevel,
        ref int roundNumber)
    {
        if (entries.Count < 2)
            return;

        var aPrefix = prefix + "A-";
        var aRoundName = GetSubBracketRoundName(entries.Count / 2);
        var aGames = new List<Game>();
        var aPoolEntries = new List<PoolEntry>();

        for (var i = 0; i < entries.Count; i += 2)
        {
            var matchNum = i / 2 + 1;
            var label = $"{aPrefix}{aRoundName}{matchNum}";

            var (homeTeamId, homePlaceholder) = ResolveAdvancingSide(entries[i]);
            var (awayTeamId, awayPlaceholder) = ResolveAdvancingSide(entries[i + 1]);

            var game = Game.Create(tournamentId, phaseId, groupId,
                round: 0,
                homeTeamId: homeTeamId, awayTeamId: awayTeamId,
                homeTeamPlaceholder: homePlaceholder, awayTeamPlaceholder: awayPlaceholder,
                label: label);
            aGames.Add(game);
            aPoolEntries.Add(new PoolEntry.Game(label));
        }

        if (entries.Count == 2)
        {
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

    private static (string? RealId, string? Placeholder) ResolveAdvancingSide(PoolEntry entry) =>
        entry switch
        {
            PoolEntry.FirstRoundBye b => (b.TeamId, null),
            PoolEntry.LoserBye lb => (null, lb.LoserPlaceholder),
            PoolEntry.Game g => (null, $"Winner {g.Label}"),
            _ => throw new ArgumentOutOfRangeException(nameof(entry), entry, "Unknown PoolEntry kind"),
        };

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

    /// <summary>
    /// A bracket-walk pool entry. Three mutually exclusive shapes:
    /// <list type="bullet">
    /// <item><see cref="Game"/> — a real played game; its winner and loser
    /// each have a referencable placeholder.</item>
    /// <item><see cref="FirstRoundBye"/> — a round-1 bye carrying the
    /// auto-advancing real team id; produces no game and has no loser.</item>
    /// <item><see cref="LoserBye"/> — a synthetic loser-side bye carrying a
    /// <c>"Loser X"</c> placeholder forward without a game; used when a
    /// sub-bracket loser pool has odd cardinality so the partnered loser
    /// auto-advances.</item>
    /// </list>
    /// </summary>
    private abstract record PoolEntry
    {
        public sealed record Game(string Label) : PoolEntry
        {
            public string LoserRef => $"Loser {Label}";
        }

        public sealed record FirstRoundBye(string TeamId) : PoolEntry;

        public sealed record LoserBye(string LoserPlaceholder) : PoolEntry;
    }
}
