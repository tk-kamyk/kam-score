using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

/// <summary>
/// Computes standings for the standard double-elimination bracket.
/// Losers Bracket round number drives position (see
/// docs/design/results-and-standings.md).
/// </summary>
public static class DoubleEliminationStandingsRanker
{
    public static List<Standing> Calculate(List<Game> games, List<string> teamIds)
    {
        var completedGames = BracketStandingsHelper.GetCompletedGames(games);

        var lbGames = completedGames.Where(g => g.Label is not null && g.Label.StartsWith("LB-")).ToList();
        var lbRoundNumbers = lbGames.Select(g => g.Round).Distinct().OrderBy(r => r).ToList();
        var totalLbRounds = lbRoundNumbers.Count;

        var teamResults = new Dictionary<string, (int Wins, int Losses, int Position)>();
        var worstPosition = teamIds.Count;

        foreach (var teamId in teamIds)
            teamResults[teamId] = (0, 0, worstPosition);

        foreach (var game in completedGames)
        {
            var result = BracketStandingsHelper.ExtractWinnerAndLoser(game);
            if (result is null) continue;
            var (winnerId, loserId) = result.Value;

            if (teamResults.TryGetValue(winnerId, out var ws))
                teamResults[winnerId] = (ws.Wins + 1, ws.Losses, ws.Position);
            if (teamResults.TryGetValue(loserId, out var ls))
                teamResults[loserId] = (ls.Wins, ls.Losses + 1, ls.Position);

            if (game.Label == "Grand Final")
            {
                if (teamResults.TryGetValue(winnerId, out var gfw))
                    teamResults[winnerId] = (gfw.Wins, gfw.Losses, 1);
                if (teamResults.TryGetValue(loserId, out var gfl))
                    teamResults[loserId] = (gfl.Wins, gfl.Losses, 2);
                continue;
            }

            if (game.Label is not null && game.Label.StartsWith("LB-"))
            {
                var roundIndex = lbRoundNumbers.IndexOf(game.Round);
                var position = totalLbRounds - roundIndex + 2;
                if (teamResults.TryGetValue(loserId, out var lbl))
                    teamResults[loserId] = (lbl.Wins, lbl.Losses, position);
            }
        }

        return BracketStandingsHelper.OrderBracketStandings(teamResults);
    }

    public static List<Standing> RankCrossGroup(List<Standing> standings)
        => BracketStandingsHelper.RankByPositionThenWins(standings);
}
