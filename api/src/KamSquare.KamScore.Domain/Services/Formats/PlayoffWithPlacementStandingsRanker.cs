using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

/// <summary>
/// Computes standings for a playoff-with-placement bracket. Positions are
/// assigned by the single-game placement rounds at the end of the bracket
/// (see docs/design/results-and-standings.md).
/// </summary>
public static class PlayoffWithPlacementStandingsRanker
{
    public static List<Standing> Calculate(List<Game> games, List<string> teamIds)
    {
        var gamesByRound = games.GroupBy(g => g.Round).ToDictionary(g => g.Key, g => g.ToList());
        var maxRound = gamesByRound.Count > 0 ? gamesByRound.Keys.Max() : 0;

        var placementRounds = new List<int>();
        for (var round = maxRound; round >= 1; round--)
        {
            if (gamesByRound.TryGetValue(round, out var roundGames) && roundGames.Count == 1)
                placementRounds.Add(round);
            else
                break;
        }

        var teamPositions = new Dictionary<string, int>();
        var teamWins = new Dictionary<string, int>();
        var teamLosses = new Dictionary<string, int>();

        foreach (var teamId in teamIds)
        {
            teamWins[teamId] = 0;
            teamLosses[teamId] = 0;
        }

        var completedGames = BracketStandingsHelper.GetCompletedGames(games);

        foreach (var game in completedGames)
        {
            var result = BracketStandingsHelper.ExtractWinnerAndLoser(game);
            if (result is null) continue;
            var (winnerId, loserId) = result.Value;

            if (teamWins.ContainsKey(winnerId)) teamWins[winnerId]++;
            if (teamLosses.ContainsKey(loserId)) teamLosses[loserId]++;
        }

        var nextPosition = 1;
        foreach (var round in placementRounds)
        {
            var game = gamesByRound[round][0];
            var placementResult = game.Status == GameStatus.Completed
                && game.HomeTeamId is not null
                && game.AwayTeamId is not null
                ? BracketStandingsHelper.ExtractWinnerAndLoser(game) : null;

            if (placementResult is not null)
            {
                teamPositions[placementResult.Value.WinnerId] = nextPosition;
                teamPositions[placementResult.Value.LoserId] = nextPosition + 1;
            }
            nextPosition += 2;
        }

        var unrankedPosition = teamIds.Count;

        return teamIds
            .Select(id =>
            {
                var position = teamPositions.GetValueOrDefault(id, unrankedPosition);
                var wins = teamWins.GetValueOrDefault(id, 0);
                var losses = teamLosses.GetValueOrDefault(id, 0);
                return BracketStandingsHelper.CreateBracketStanding(id, position, wins, losses);
            })
            .OrderBy(s => s.Position)
            .ThenByDescending(s => s.Wins)
            .ToList();
    }

    public static List<Standing> RankCrossGroup(List<Standing> standings)
        => BracketStandingsHelper.RankByPositionThenWins(standings);
}
