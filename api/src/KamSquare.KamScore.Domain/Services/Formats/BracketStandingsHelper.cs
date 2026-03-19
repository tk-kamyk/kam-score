using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

internal static class BracketStandingsHelper
{
    internal static List<Game> GetCompletedGames(List<Game> games)
    {
        return games
            .Where(g => g.Status == GameStatus.Completed
                        && g.HomeTeamId is not null
                        && g.AwayTeamId is not null)
            .ToList();
    }

    internal static (string WinnerId, string LoserId)? ExtractWinnerAndLoser(Game game)
    {
        var homeScore = game.HomeScore ?? 0;
        var awayScore = game.AwayScore ?? 0;
        if (homeScore == awayScore) return null;

        return homeScore > awayScore
            ? (game.HomeTeamId!, game.AwayTeamId!)
            : (game.AwayTeamId!, game.HomeTeamId!);
    }

    internal static Standing CreateBracketStanding(string teamId, int position, int wins, int losses)
    {
        return new Standing(
            teamId,
            position,
            wins + losses,
            wins,
            0,
            losses,
            null, null, null, null,
            null, null, null);
    }

    internal static List<Standing> OrderBracketStandings(
        Dictionary<string, (int Wins, int Losses, int Position)> teamResults)
    {
        return teamResults
            .OrderBy(t => t.Value.Position)
            .ThenByDescending(t => t.Value.Wins)
            .Select(t => CreateBracketStanding(t.Key, t.Value.Position, t.Value.Wins, t.Value.Losses))
            .ToList();
    }

    internal static List<Standing> RankByPositionThenWins(List<Standing> standings)
    {
        return standings
            .OrderBy(s => s.Position)
            .ThenByDescending(s => s.Wins)
            .ToList();
    }
}
