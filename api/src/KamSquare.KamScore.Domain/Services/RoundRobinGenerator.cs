using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services;

public static class RoundRobinGenerator
{
    /// <summary>
    /// Generates round-robin games for a group using the circle method.
    /// Home/away is balanced per team using a tracking approach.
    /// Referee for each game: the available team (not playing) with fewest referee duties.
    /// </summary>
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

        // Add BYE placeholder if odd number of teams
        if (isOdd)
            teams.Add("BYE");

        var n = teams.Count;
        var totalRounds = n - 1;

        // Track home game counts per team for balancing
        var homeCount = teams.ToDictionary(t => t, _ => 0);
        // Track referee counts for balanced distribution
        var refCount = teamIds.ToDictionary(t => t, _ => 0);

        // Circle method: fix teams[0], rotate the rest
        var rotating = teams.Skip(1).ToList();

        for (var round = 0; round < totalRounds; round++)
        {
            var roundNumber = round + 1;

            // Build current round positions
            var currentTeams = new List<string> { teams[0] };
            currentTeams.AddRange(rotating);

            var pairings = new List<(string home, string away)>();
            for (var i = 0; i < n / 2; i++)
            {
                var team1 = currentTeams[i];
                var team2 = currentTeams[n - 1 - i];

                // Assign home/away based on who has fewer home games
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

            // Create games, skip BYE pairings, assign referee per game
            foreach (var (home, away) in pairings)
            {
                if (home == "BYE" || away == "BYE") continue;

                // Pick referee: available team with fewest referee duties
                var referee = teamIds
                    .Where(t => t != home && t != away)
                    .MinBy(t => refCount[t]);

                if (referee is not null)
                    refCount[referee]++;

                games.Add(Game.Create(
                    tournamentId, phaseId, groupId, roundNumber,
                    homeTeamId: home, awayTeamId: away,
                    refereeTeamId: referee));
            }

            // Rotate: move last element to second position
            var last = rotating[^1];
            rotating.RemoveAt(rotating.Count - 1);
            rotating.Insert(0, last);
        }

        return games;
    }
}
