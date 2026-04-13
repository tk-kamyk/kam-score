using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services.Formats;

/// <summary>
/// Generates round-robin pairings using the circle method with home/away
/// balance. See docs/design/game-generation.md.
/// </summary>
public static class RoundRobinGenerator
{
    private const string ByePlaceholder = "BYE";

    public static List<Game> Generate(
        string tournamentId,
        string phaseId,
        string groupId,
        List<string> teamIds)
    {
        if (teamIds.Count <= 1)
            return [];

        var games = new List<Game>();
        var teams = new List<string>(teamIds);
        var isOdd = teams.Count % 2 != 0;

        if (isOdd)
            teams.Add(ByePlaceholder);

        var n = teams.Count;
        var totalRounds = n - 1;
        var homeCount = teams.ToDictionary(t => t, _ => 0);
        var rotating = teams.Skip(1).ToList();

        for (var round = 0; round < totalRounds; round++)
        {
            var roundNumber = round + 1;
            var currentTeams = new List<string> { teams[0] };
            currentTeams.AddRange(rotating);

            var pairings = new List<(string home, string away)>();
            for (var i = 0; i < n / 2; i++)
            {
                var team1 = currentTeams[i];
                var team2 = currentTeams[n - 1 - i];

                if (homeCount[team1] <= homeCount[team2])
                {
                    pairings.Add((team1, team2));
                    homeCount[team1]++;
                }
                else
                {
                    pairings.Add((team2, team1));
                    homeCount[team2]++;
                }
            }

            foreach (var (home, away) in pairings)
            {
                if (home == ByePlaceholder || away == ByePlaceholder) continue;

                games.Add(Game.Create(
                    tournamentId, phaseId, groupId, roundNumber,
                    homeTeamId: home, awayTeamId: away));
            }

            var last = rotating[^1];
            rotating.RemoveAt(rotating.Count - 1);
            rotating.Insert(0, last);
        }

        return games;
    }
}
