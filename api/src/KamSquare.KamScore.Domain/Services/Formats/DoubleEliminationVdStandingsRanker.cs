using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

/// <summary>
/// Maps completed games in the VD double-elimination bracket to positions
/// (1st, 2nd, shared 3rd, shared 5th, 7th, 8th). See
/// docs/design/results-and-standings.md.
/// </summary>
public static class DoubleEliminationVdStandingsRanker
{
    public static List<Standing> Calculate(List<Game> games, List<string> teamIds)
    {
        var completedGames = BracketStandingsHelper.GetCompletedGames(games);
        var worstPosition = teamIds.Count;

        var teamResults = teamIds.ToDictionary(id => id, _ => (Wins: 0, Losses: 0, Position: worstPosition));

        foreach (var game in completedGames)
        {
            var result = BracketStandingsHelper.ExtractWinnerAndLoser(game);
            if (result is null) continue;
            var (winnerId, loserId) = result.Value;

            if (teamResults.TryGetValue(winnerId, out var ws))
                teamResults[winnerId] = (ws.Wins + 1, ws.Losses, ws.Position);
            if (teamResults.TryGetValue(loserId, out var ls))
                teamResults[loserId] = (ls.Wins, ls.Losses + 1, ls.Position);

            switch (game.Label)
            {
                case "Grand Final":
                    if (teamResults.ContainsKey(winnerId))
                        teamResults[winnerId] = (teamResults[winnerId].Wins, teamResults[winnerId].Losses, 1);
                    if (teamResults.ContainsKey(loserId))
                        teamResults[loserId] = (teamResults[loserId].Wins, teamResults[loserId].Losses, 2);
                    break;
                case "GSF1" or "GSF2":
                    if (teamResults.ContainsKey(loserId))
                        teamResults[loserId] = (teamResults[loserId].Wins, teamResults[loserId].Losses, 3);
                    break;
                case "X1" or "X2":
                    if (teamResults.ContainsKey(loserId))
                        teamResults[loserId] = (teamResults[loserId].Wins, teamResults[loserId].Losses, 5);
                    break;
                case "7th Place":
                    if (teamResults.ContainsKey(winnerId))
                        teamResults[winnerId] = (teamResults[winnerId].Wins, teamResults[winnerId].Losses, 7);
                    if (teamResults.ContainsKey(loserId))
                        teamResults[loserId] = (teamResults[loserId].Wins, teamResults[loserId].Losses, 8);
                    break;
            }
        }

        return BracketStandingsHelper.OrderBracketStandings(teamResults);
    }

    public static List<Standing> RankCrossGroup(List<Standing> standings)
        => BracketStandingsHelper.RankByPositionThenWins(standings);
}
