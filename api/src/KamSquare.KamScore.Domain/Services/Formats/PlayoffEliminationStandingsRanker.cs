using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

/// <summary>
/// Computes standings for single-elimination brackets. Positions are derived
/// from the round in which a team lost (see docs/design/results-and-standings.md).
/// </summary>
public static class PlayoffEliminationStandingsRanker
{
    public static List<Standing> Calculate(List<Game> games, List<string> teamIds)
    {
        var bracketSize = BracketUtilities.NextPowerOfTwo(teamIds.Count);
        var completedGames = BracketStandingsHelper.GetCompletedGames(games);

        var maxRound = completedGames.Count > 0 ? completedGames.Max(g => g.Round) : 0;
        var teamResults = new Dictionary<string, (int Wins, int Losses, int Position)>();

        var worstPosition = bracketSize / 2 + 1;
        foreach (var teamId in teamIds)
            teamResults[teamId] = (0, 0, worstPosition);

        foreach (var game in completedGames)
        {
            var result = BracketStandingsHelper.ExtractWinnerAndLoser(game);
            if (result is null) continue;
            var (winnerId, loserId) = result.Value;

            if (teamResults.TryGetValue(winnerId, out var winnerStats))
                teamResults[winnerId] = (winnerStats.Wins + 1, winnerStats.Losses, winnerStats.Position);
            if (teamResults.TryGetValue(loserId, out var loserStats))
                teamResults[loserId] = (loserStats.Wins, loserStats.Losses + 1, loserStats.Position);

            var loserPosition = bracketSize / (int)Math.Pow(2, game.Round) + 1;
            if (teamResults.TryGetValue(loserId, out var ls))
                teamResults[loserId] = (ls.Wins, ls.Losses, loserPosition);

            if (game.Round == maxRound && teamResults.TryGetValue(winnerId, out var ws))
                teamResults[winnerId] = (ws.Wins, ws.Losses, 1);
        }

        return BracketStandingsHelper.OrderBracketStandings(teamResults);
    }

    public static List<Standing> RankCrossGroup(List<Standing> standings)
        => BracketStandingsHelper.RankByPositionThenWins(standings);
}
