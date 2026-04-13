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
}
