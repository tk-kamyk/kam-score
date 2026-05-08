using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services.Formats;

/// <summary>
/// Generates a single-elimination bracket for a group. See
/// docs/design/game-generation.md.
/// </summary>
public static class PlayoffEliminationGenerator
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
                    homeTeamPlaceholder: null, awayTeamPlaceholder: null,
                    label: "Final")
            ];
        }

        var bracketSize = BracketUtilities.NextPowerOfTwo(teamIds.Count);
        var totalRounds = (int)Math.Log2(bracketSize);
        var roundNames = BracketUtilities.GetRoundNames(totalRounds);

        var (firstRoundGames, firstRoundSlots) = BracketUtilities.BuildFirstRoundPool(
            tournamentId, phaseId, groupId, teamIds, bracketSize, roundNames[0]);

        var games = new List<Game>(firstRoundGames);

        // Bye-last reorder: round-2 pair groupings are reordered so the bye
        // seed's pair (mixed: 1 phantom + 1 real game) ends up in the last slot.
        var orderedFirstRound = BracketUtilities.ReorderPairsByByeLast(
            firstRoundSlots, s => s is BracketUtilities.FirstRoundSlot.Bye);

        // Generate round 2 from the reordered first-round slots.
        var prevRoundGames = new List<Game>();
        var matchesInRound2 = bracketSize / 4;
        for (var match = 0; match < matchesInRound2; match++)
        {
            var (homeTeamId, homePlaceholder) = ResolveSlot(orderedFirstRound[match * 2]);
            var (awayTeamId, awayPlaceholder) = ResolveSlot(orderedFirstRound[match * 2 + 1]);

            var matchLabel = BracketUtilities.GetMatchLabel(roundNames[1], match + 1);
            var game = Game.Create(tournamentId, phaseId, groupId, round: 2,
                homeTeamId: homeTeamId, awayTeamId: awayTeamId,
                homeTeamPlaceholder: homePlaceholder, awayTeamPlaceholder: awayPlaceholder,
                label: matchLabel);
            games.Add(game);
            prevRoundGames.Add(game);
        }

        // Rounds 3..totalRounds: pair previous-round games (no phantoms exist in round 2+).
        for (var round = 3; round <= totalRounds; round++)
        {
            var matchesInRound = bracketSize / (int)Math.Pow(2, round);
            var thisRoundGames = new List<Game>(matchesInRound);

            for (var match = 0; match < matchesInRound; match++)
            {
                var prev1 = prevRoundGames[match * 2];
                var prev2 = prevRoundGames[match * 2 + 1];

                var matchLabel = BracketUtilities.GetMatchLabel(roundNames[round - 1], match + 1);
                var game = Game.Create(tournamentId, phaseId, groupId, round: round,
                    homeTeamId: null, awayTeamId: null,
                    homeTeamPlaceholder: $"Winner {prev1.Label}",
                    awayTeamPlaceholder: $"Winner {prev2.Label}",
                    label: matchLabel);
                games.Add(game);
                thisRoundGames.Add(game);
            }

            prevRoundGames = thisRoundGames;
        }

        return games;
    }

    private static (string? RealId, string? Placeholder) ResolveSlot(BracketUtilities.FirstRoundSlot slot) =>
        slot switch
        {
            BracketUtilities.FirstRoundSlot.Bye b => (b.TeamId, null),
            BracketUtilities.FirstRoundSlot.Real r => (null, $"Winner {r.GameLabel}"),
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown FirstRoundSlot kind"),
        };
}
