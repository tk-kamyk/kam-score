using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services.Formats;

/// <summary>
/// Generates the volleyball-specific VD double-elimination bracket for exactly
/// 8 teams. See docs/design/game-generation.md for the full 14-game layout.
/// </summary>
public static class DoubleEliminationVdGenerator
{
    public const int RequiredTeamCount = 8;

    public static List<Game> Generate(
        string tournamentId,
        string phaseId,
        string groupId,
        List<string> teamIds)
    {
        if (teamIds.Count != RequiredTeamCount)
            throw new InvalidOperationException(
                $"Double Elimination (VD) requires exactly {RequiredTeamCount} teams, but got {teamIds.Count}.");

        var games = new List<Game>();
        var bracketOrder = BracketUtilities.BuildBracketOrder(RequiredTeamCount);

        // R1: Quarter-Finals
        var qfPairings = new (int home, int away)[]
        {
            (bracketOrder[0], bracketOrder[1]),
            (bracketOrder[2], bracketOrder[3]),
            (bracketOrder[4], bracketOrder[5]),
            (bracketOrder[6], bracketOrder[7])
        };

        for (var i = 0; i < 4; i++)
        {
            var (home, away) = qfPairings[i];
            games.Add(Game.Create(tournamentId, phaseId, groupId, round: 1,
                homeTeamId: teamIds[home], awayTeamId: teamIds[away],
                label: $"QF{i + 1}"));
        }

        // R2: QF Winners
        games.Add(Game.Create(tournamentId, phaseId, groupId, round: 2,
            homeTeamPlaceholder: "Winner QF1", awayTeamPlaceholder: "Winner QF2",
            label: "W1"));
        games.Add(Game.Create(tournamentId, phaseId, groupId, round: 2,
            homeTeamPlaceholder: "Winner QF3", awayTeamPlaceholder: "Winner QF4",
            label: "W2"));

        // R3: QF Losers
        games.Add(Game.Create(tournamentId, phaseId, groupId, round: 3,
            homeTeamPlaceholder: "Loser QF1", awayTeamPlaceholder: "Loser QF2",
            label: "L1"));
        games.Add(Game.Create(tournamentId, phaseId, groupId, round: 3,
            homeTeamPlaceholder: "Loser QF3", awayTeamPlaceholder: "Loser QF4",
            label: "L2"));

        // R4: Crossover
        games.Add(Game.Create(tournamentId, phaseId, groupId, round: 4,
            homeTeamPlaceholder: "Loser W2", awayTeamPlaceholder: "Winner L1",
            label: "X1"));
        games.Add(Game.Create(tournamentId, phaseId, groupId, round: 4,
            homeTeamPlaceholder: "Loser W1", awayTeamPlaceholder: "Winner L2",
            label: "X2"));

        // R5: Grand Semi-Finals
        games.Add(Game.Create(tournamentId, phaseId, groupId, round: 5,
            homeTeamPlaceholder: "Winner W1", awayTeamPlaceholder: "Winner X1",
            label: "GSF1"));
        games.Add(Game.Create(tournamentId, phaseId, groupId, round: 5,
            homeTeamPlaceholder: "Winner W2", awayTeamPlaceholder: "Winner X2",
            label: "GSF2"));

        // R6: 7th Place
        games.Add(Game.Create(tournamentId, phaseId, groupId, round: 6,
            homeTeamPlaceholder: "Loser L1", awayTeamPlaceholder: "Loser L2",
            label: "7th Place"));

        // R7: Grand Final
        games.Add(Game.Create(tournamentId, phaseId, groupId, round: 7,
            homeTeamPlaceholder: "Winner GSF1", awayTeamPlaceholder: "Winner GSF2",
            label: "Grand Final"));

        return games;
    }
}
