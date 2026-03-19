using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public class RoundRobinStrategy : IPhaseFormatStrategy
{
    private const string ByePlaceholder = "BYE";

    public bool SupportsRefereeAssignment => true;

    public void ValidateTeams(List<Group> groups)
    {
        // No format-specific team count constraints for round robin
    }

    public List<Game> GenerateGames(
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

    public List<Standing> CalculateStandings(List<Game> games, List<string> teamIds)
    {
        var completedGames = BracketStandingsHelper.GetCompletedGames(games);
        var stats = BuildTeamStatistics(completedGames, teamIds);

        var entries = teamIds.Select(id => new RoundRobinEntry(id, stats[id])).ToList();

        entries.Sort((a, b) =>
        {
            var cmp = b.Stats.Points.CompareTo(a.Stats.Points);
            if (cmp != 0) return cmp;
            cmp = b.Stats.SetDifference.CompareTo(a.Stats.SetDifference);
            if (cmp != 0) return cmp;
            return b.Stats.PointDifference.CompareTo(a.Stats.PointDifference);
        });

        entries = ApplyDirectResultTiebreaker(entries, completedGames);
        entries = ApplyPointsScoredTiebreaker(entries);

        return AssignPositions(entries);
    }

    public List<Standing> RankCrossGroup(List<Standing> standings)
    {
        return standings
            .OrderByDescending(s => s.Points ?? 0)
            .ThenByDescending(s => s.SetDifference ?? 0)
            .ThenByDescending(s => s.PointDifference ?? 0)
            .ThenByDescending(s => s.Wins)
            .ThenBy(s => s.Losses)
            .ToList();
    }

    private static Dictionary<string, TeamStats> BuildTeamStatistics(
        List<Game> completedGames, List<string> teamIds)
    {
        var stats = teamIds.ToDictionary(id => id, _ => new TeamStats());

        foreach (var game in completedGames)
        {
            var home = stats.GetValueOrDefault(game.HomeTeamId!);
            var away = stats.GetValueOrDefault(game.AwayTeamId!);
            if (home is null || away is null) continue;

            AccumulateGameStats(home, away, game);
        }

        return stats;
    }

    private static void AccumulateGameStats(TeamStats home, TeamStats away, Game game)
    {
        home.GamesPlayed++;
        away.GamesPlayed++;

        var homeScore = game.HomeScore ?? 0;
        var awayScore = game.AwayScore ?? 0;

        home.SetsWon += homeScore;
        home.SetsLost += awayScore;
        away.SetsWon += awayScore;
        away.SetsLost += homeScore;

        if (game.Sets is not null)
        {
            foreach (var set in game.Sets)
            {
                home.PointsWon += set.HomePoints;
                home.PointsLost += set.AwayPoints;
                away.PointsWon += set.AwayPoints;
                away.PointsLost += set.HomePoints;
            }
        }

        if (homeScore > awayScore)
        {
            home.Wins++;
            away.Losses++;
        }
        else if (awayScore > homeScore)
        {
            away.Wins++;
            home.Losses++;
        }
        else
        {
            home.Draws++;
            away.Draws++;
        }
    }

    private static List<RoundRobinEntry> ApplyDirectResultTiebreaker(
        List<RoundRobinEntry> sortedEntries, List<Game> completedGames)
    {
        return ResolveTiedGroups(sortedEntries, tiedGroup => ResolveDirectResult(tiedGroup, completedGames));
    }

    private static List<RoundRobinEntry> ApplyPointsScoredTiebreaker(
        List<RoundRobinEntry> entries)
    {
        return ResolveTiedGroups(entries, tiedGroup =>
        {
            tiedGroup.Sort((a, b) => b.Stats.PointsWon.CompareTo(a.Stats.PointsWon));
            return tiedGroup;
        });
    }

    private static List<RoundRobinEntry> ResolveTiedGroups(
        List<RoundRobinEntry> entries,
        Func<List<RoundRobinEntry>, List<RoundRobinEntry>> resolveTie)
    {
        var result = new List<RoundRobinEntry>();
        var i = 0;

        while (i < entries.Count)
        {
            var j = i + 1;
            while (j < entries.Count
                   && entries[j].Stats.Points == entries[i].Stats.Points
                   && entries[j].Stats.SetDifference == entries[i].Stats.SetDifference
                   && entries[j].Stats.PointDifference == entries[i].Stats.PointDifference)
            {
                j++;
            }

            if (j - i > 1)
            {
                var tiedGroup = entries.GetRange(i, j - i);
                result.AddRange(resolveTie(tiedGroup));
            }
            else
            {
                result.Add(entries[i]);
            }

            i = j;
        }

        return result;
    }

    private static List<RoundRobinEntry> ResolveDirectResult(
        List<RoundRobinEntry> tiedEntries, List<Game> completedGames)
    {
        if (tiedEntries.Count <= 1)
            return tiedEntries;

        var tiedIds = tiedEntries.Select(e => e.TeamId).ToHashSet();

        var miniStats = tiedIds.ToDictionary(id => id, _ => new TeamStats());
        var h2hGames = completedGames
            .Where(g => tiedIds.Contains(g.HomeTeamId!) && tiedIds.Contains(g.AwayTeamId!))
            .ToList();

        foreach (var game in h2hGames)
        {
            var home = miniStats[game.HomeTeamId!];
            var away = miniStats[game.AwayTeamId!];

            AccumulateGameStats(home, away, game);
        }

        var sorted = tiedEntries.ToList();
        sorted.Sort((a, b) =>
        {
            var aH2H = miniStats[a.TeamId];
            var bH2H = miniStats[b.TeamId];

            var cmp = bH2H.Points.CompareTo(aH2H.Points);
            if (cmp != 0) return cmp;
            cmp = bH2H.SetDifference.CompareTo(aH2H.SetDifference);
            if (cmp != 0) return cmp;
            return bH2H.PointDifference.CompareTo(aH2H.PointDifference);
        });

        return sorted;
    }

    private static List<Standing> AssignPositions(List<RoundRobinEntry> entries)
    {
        var standings = new List<Standing>();
        var position = 1;

        for (var i = 0; i < entries.Count; i++)
        {
            if (i > 0)
            {
                var prev = entries[i - 1].Stats;
                var curr = entries[i].Stats;

                if (curr.Points != prev.Points
                    || curr.SetDifference != prev.SetDifference
                    || curr.PointDifference != prev.PointDifference
                    || curr.PointsWon != prev.PointsWon)
                {
                    position = i + 1;
                }
            }

            var s = entries[i].Stats;
            standings.Add(new Standing(
                entries[i].TeamId,
                position,
                s.GamesPlayed,
                s.Wins,
                s.Draws,
                s.Losses,
                s.Points,
                s.SetsWon,
                s.SetsLost,
                s.SetDifference,
                s.PointsWon,
                s.PointsLost,
                s.PointDifference));
        }

        return standings;
    }

    private class TeamStats
    {
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int SetsWon { get; set; }
        public int SetsLost { get; set; }
        public int PointsWon { get; set; }
        public int PointsLost { get; set; }
        public int Points => 2 * Wins + Draws;
        public int SetDifference => SetsWon - SetsLost;
        public int PointDifference => PointsWon - PointsLost;
    }

    private record RoundRobinEntry(string TeamId, TeamStats Stats);
}
