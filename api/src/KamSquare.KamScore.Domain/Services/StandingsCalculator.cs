using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services;

public static class StandingsCalculator
{
    public static List<Standing> Calculate(PhaseFormat format, List<Game> games, List<string> teamIds)
    {
        return format switch
        {
            PhaseFormat.RoundRobin => CalculateRoundRobin(games, teamIds),
            PhaseFormat.PlayoffElimination => CalculatePlayoffElimination(games, teamIds),
            PhaseFormat.PlayoffWithPlacement => CalculatePlayoffWithPlacement(games, teamIds),
            _ => []
        };
    }

    public static List<Standing> CalculateRoundRobin(List<Game> games, List<string> teamIds)
    {
        var stats = teamIds.ToDictionary(id => id, _ => new TeamStats());
        var completedGames = games
            .Where(g => g.Status == GameStatus.Completed
                        && g.HomeTeamId is not null
                        && g.AwayTeamId is not null)
            .ToList();

        foreach (var game in completedGames)
        {
            var home = stats.GetValueOrDefault(game.HomeTeamId!);
            var away = stats.GetValueOrDefault(game.AwayTeamId!);
            if (home is null || away is null) continue;

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

        var entries = teamIds.Select(id =>
        {
            var s = stats[id];
            return new RoundRobinEntry(id, s);
        }).ToList();

        // Sort by Points desc, SetDifference desc
        entries.Sort((a, b) =>
        {
            var cmp = b.Stats.Points.CompareTo(a.Stats.Points);
            if (cmp != 0) return cmp;
            return b.Stats.SetDifference.CompareTo(a.Stats.SetDifference);
        });

        // Apply direct result tiebreaker for tied groups
        entries = ApplyDirectResultTiebreaker(entries, completedGames);

        // Assign positions (tied teams share position)
        return AssignRoundRobinPositions(entries);
    }

    public static List<Standing> CalculatePlayoffElimination(List<Game> games, List<string> teamIds)
    {
        var bracketSize = BracketUtilities.NextPowerOfTwo(teamIds.Count);
        var completedGames = games
            .Where(g => g.Status == GameStatus.Completed
                        && g.HomeTeamId is not null
                        && g.AwayTeamId is not null)
            .ToList();

        var maxRound = completedGames.Count > 0 ? completedGames.Max(g => g.Round) : 0;
        var teamResults = new Dictionary<string, (int Wins, int Losses, int Position)>();

        // Initialize all teams at worst position
        var worstPosition = bracketSize / 2 + 1;
        foreach (var teamId in teamIds)
        {
            teamResults[teamId] = (0, 0, worstPosition);
        }

        foreach (var game in completedGames)
        {
            var homeScore = game.HomeScore ?? 0;
            var awayScore = game.AwayScore ?? 0;

            string winnerId, loserId;
            if (homeScore > awayScore)
            {
                winnerId = game.HomeTeamId!;
                loserId = game.AwayTeamId!;
            }
            else
            {
                winnerId = game.AwayTeamId!;
                loserId = game.HomeTeamId!;
            }

            // Update win/loss counts
            if (teamResults.TryGetValue(winnerId, out var winnerStats))
                teamResults[winnerId] = (winnerStats.Wins + 1, winnerStats.Losses, winnerStats.Position);
            if (teamResults.TryGetValue(loserId, out var loserStats))
                teamResults[loserId] = (loserStats.Wins, loserStats.Losses + 1, loserStats.Position);

            // Loser position = bracketSize / 2^round + 1
            var loserPosition = bracketSize / (int)Math.Pow(2, game.Round) + 1;
            if (teamResults.TryGetValue(loserId, out var ls))
                teamResults[loserId] = (ls.Wins, ls.Losses, loserPosition);

            // Winner of the highest round = position 1
            if (game.Round == maxRound && teamResults.TryGetValue(winnerId, out var ws))
                teamResults[winnerId] = (ws.Wins, ws.Losses, 1);
        }

        return teamResults
            .OrderBy(t => t.Value.Position)
            .ThenByDescending(t => t.Value.Wins)
            .Select(t => new Standing(
                t.Key,
                t.Value.Position,
                t.Value.Wins + t.Value.Losses,
                t.Value.Wins,
                0,
                t.Value.Losses,
                null, null, null, null,
                null, null, null))
            .ToList();
    }

    public static List<Standing> CalculatePlayoffWithPlacement(List<Game> games, List<string> teamIds)
    {
        // Identify placement rounds: contiguous highest rounds with exactly 1 game each
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

        // placementRounds is descending (Final first, then 3rd place, etc.)
        var teamPositions = new Dictionary<string, int>();
        var teamWins = new Dictionary<string, int>();
        var teamLosses = new Dictionary<string, int>();

        foreach (var teamId in teamIds)
        {
            teamWins[teamId] = 0;
            teamLosses[teamId] = 0;
        }

        // Count wins/losses from all completed games with real team IDs
        var completedGames = games
            .Where(g => g.Status == GameStatus.Completed
                        && g.HomeTeamId is not null
                        && g.AwayTeamId is not null)
            .ToList();

        foreach (var game in completedGames)
        {
            var homeScore = game.HomeScore ?? 0;
            var awayScore = game.AwayScore ?? 0;

            if (homeScore > awayScore)
            {
                if (teamWins.ContainsKey(game.HomeTeamId!)) teamWins[game.HomeTeamId!]++;
                if (teamLosses.ContainsKey(game.AwayTeamId!)) teamLosses[game.AwayTeamId!]++;
            }
            else
            {
                if (teamWins.ContainsKey(game.AwayTeamId!)) teamWins[game.AwayTeamId!]++;
                if (teamLosses.ContainsKey(game.HomeTeamId!)) teamLosses[game.HomeTeamId!]++;
            }
        }

        // Assign positions from placement games (highest round = Final = 1st/2nd)
        var nextPosition = 1;
        foreach (var round in placementRounds)
        {
            var game = gamesByRound[round][0];
            if (game.Status == GameStatus.Completed
                && game.HomeTeamId is not null
                && game.AwayTeamId is not null)
            {
                var homeScore = game.HomeScore ?? 0;
                var awayScore = game.AwayScore ?? 0;

                string winnerId, loserId;
                if (homeScore > awayScore)
                {
                    winnerId = game.HomeTeamId!;
                    loserId = game.AwayTeamId!;
                }
                else
                {
                    winnerId = game.AwayTeamId!;
                    loserId = game.HomeTeamId!;
                }

                teamPositions[winnerId] = nextPosition;
                teamPositions[loserId] = nextPosition + 1;
            }
            nextPosition += 2;
        }

        // Teams without placement results get unranked (position = teamIds.Count)
        var unrankedPosition = teamIds.Count;

        return teamIds
            .Select(id =>
            {
                var position = teamPositions.GetValueOrDefault(id, unrankedPosition);
                var wins = teamWins.GetValueOrDefault(id, 0);
                var losses = teamLosses.GetValueOrDefault(id, 0);
                return new Standing(
                    id,
                    position,
                    wins + losses,
                    wins,
                    0,
                    losses,
                    null, null, null, null,
                    null, null, null);
            })
            .OrderBy(s => s.Position)
            .ThenByDescending(s => s.Wins)
            .ToList();
    }

    private static List<RoundRobinEntry> ApplyDirectResultTiebreaker(
        List<RoundRobinEntry> sortedEntries, List<Game> completedGames)
    {
        var result = new List<RoundRobinEntry>();
        var i = 0;

        while (i < sortedEntries.Count)
        {
            // Find group of tied teams (same points and set difference)
            var j = i + 1;
            while (j < sortedEntries.Count
                   && sortedEntries[j].Stats.Points == sortedEntries[i].Stats.Points
                   && sortedEntries[j].Stats.SetDifference == sortedEntries[i].Stats.SetDifference)
            {
                j++;
            }

            if (j - i > 1)
            {
                // Multiple teams tied — apply head-to-head tiebreaker
                var tiedGroup = sortedEntries.GetRange(i, j - i);
                var resolved = ResolveDirectResult(tiedGroup, completedGames);
                result.AddRange(resolved);
            }
            else
            {
                result.Add(sortedEntries[i]);
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

        // Build mini-standings from head-to-head games only
        var miniStats = tiedIds.ToDictionary(id => id, _ => new TeamStats());
        var h2hGames = completedGames
            .Where(g => tiedIds.Contains(g.HomeTeamId!) && tiedIds.Contains(g.AwayTeamId!))
            .ToList();

        foreach (var game in h2hGames)
        {
            var home = miniStats[game.HomeTeamId!];
            var away = miniStats[game.AwayTeamId!];

            var homeScore = game.HomeScore ?? 0;
            var awayScore = game.AwayScore ?? 0;

            home.SetsWon += homeScore;
            home.SetsLost += awayScore;
            away.SetsWon += awayScore;
            away.SetsLost += homeScore;

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

        // Sort by h2h points, then h2h set difference
        var sorted = tiedEntries.ToList();
        sorted.Sort((a, b) =>
        {
            var aH2H = miniStats[a.TeamId];
            var bH2H = miniStats[b.TeamId];

            var cmp = bH2H.Points.CompareTo(aH2H.Points);
            if (cmp != 0) return cmp;
            return bH2H.SetDifference.CompareTo(aH2H.SetDifference);
        });

        return sorted;
    }

    private static List<Standing> AssignRoundRobinPositions(List<RoundRobinEntry> entries)
    {
        var standings = new List<Standing>();
        var position = 1;

        for (var i = 0; i < entries.Count; i++)
        {
            if (i > 0)
            {
                var prev = entries[i - 1].Stats;
                var curr = entries[i].Stats;

                // Same position if all sort criteria match
                if (curr.Points != prev.Points
                    || curr.SetDifference != prev.SetDifference)
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
