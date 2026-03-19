using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public class PlayoffEliminationStrategy : IPhaseFormatStrategy
{
    public bool SupportsRefereeAssignment => false;

    public void ValidateTeams(List<Group> groups)
    {
        // No format-specific team count constraints for single elimination
    }

    public List<Game> GenerateGames(
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

        var games = new List<Game>();
        var n = teamIds.Count;
        var bracketSize = BracketUtilities.NextPowerOfTwo(n);
        var totalRounds = (int)Math.Log2(bracketSize);

        var bracketOrder = BracketUtilities.BuildBracketOrder(bracketSize);
        var gameMap = new Dictionary<(int round, int matchIndex), Game>();
        var roundNames = BracketUtilities.GetRoundNames(totalRounds);

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
                var advancingTeamId = team1 ?? team2;
                var phantomGame = Game.Create(tournamentId, phaseId, groupId, round: 1,
                    homeTeamId: advancingTeamId);
                gameMap[(1, match)] = phantomGame;
                continue;
            }

            gameIndex++;
            var matchLabel = BracketUtilities.GetMatchLabel(roundNames[0], gameIndex);
            var game = Game.Create(tournamentId, phaseId, groupId, round: 1,
                homeTeamId: team1, awayTeamId: team2, label: matchLabel);
            games.Add(game);
            gameMap[(1, match)] = game;
        }

        for (var round = 2; round <= totalRounds; round++)
        {
            var matchesInRound = bracketSize / (int)Math.Pow(2, round);
            gameIndex = 0;

            for (var match = 0; match < matchesInRound; match++)
            {
                gameIndex++;
                var prevMatch1 = match * 2;
                var prevMatch2 = match * 2 + 1;

                var prevGame1 = gameMap.GetValueOrDefault((round - 1, prevMatch1));
                var prevGame2 = gameMap.GetValueOrDefault((round - 1, prevMatch2));

                string? homeTeamId = null;
                string? awayTeamId = null;
                string? homePlaceholder = null;
                string? awayPlaceholder = null;

                if (prevGame1 is not null && prevGame1.AwayTeamId is null && prevGame1.HomeTeamId is not null)
                    homeTeamId = prevGame1.HomeTeamId;
                else
                    homePlaceholder = $"Winner {BracketUtilities.GetMatchLabel(roundNames[round - 2], prevMatch1 + 1)}";

                if (prevGame2 is not null && prevGame2.AwayTeamId is null && prevGame2.HomeTeamId is not null)
                    awayTeamId = prevGame2.HomeTeamId;
                else
                    awayPlaceholder = $"Winner {BracketUtilities.GetMatchLabel(roundNames[round - 2], prevMatch2 + 1)}";

                var matchLabel = BracketUtilities.GetMatchLabel(roundNames[round - 1], gameIndex);
                var game = Game.Create(tournamentId, phaseId, groupId, round: round,
                    homeTeamId: homeTeamId, awayTeamId: awayTeamId,
                    homeTeamPlaceholder: homePlaceholder, awayTeamPlaceholder: awayPlaceholder,
                    label: matchLabel);
                games.Add(game);
                gameMap[(round, match)] = game;
            }
        }

        return games;
    }

    public List<Standing> CalculateStandings(List<Game> games, List<string> teamIds)
    {
        var bracketSize = BracketUtilities.NextPowerOfTwo(teamIds.Count);
        var completedGames = BracketStandingsHelper.GetCompletedGames(games);

        var maxRound = completedGames.Count > 0 ? completedGames.Max(g => g.Round) : 0;
        var teamResults = new Dictionary<string, (int Wins, int Losses, int Position)>();

        var worstPosition = bracketSize / 2 + 1;
        foreach (var teamId in teamIds)
        {
            teamResults[teamId] = (0, 0, worstPosition);
        }

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

    public List<Standing> RankCrossGroup(List<Standing> standings)
    {
        return BracketStandingsHelper.RankByPositionThenWins(standings);
    }
}
