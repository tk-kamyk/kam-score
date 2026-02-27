using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Domain.UnitTest;

public class GameSchedulerTests
{
    private static readonly DateTime StartTime = new(2026, 6, 1, 9, 0, 0);
    private const int GameLength = 30;

    [Fact]
    public void Schedule_EmptyGames_ShouldReturnEmpty()
    {
        var result = GameScheduler.Schedule([], ["c1"], StartTime, GameLength);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Schedule_EmptyCourts_ShouldReturnGamesUnchanged()
    {
        var games = new List<Game>
        {
            Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b")
        };

        var result = GameScheduler.Schedule(games, [], StartTime, GameLength);
        result.Should().AllSatisfy(g => g.CourtId.Should().BeNull());
    }

    [Fact]
    public void Schedule_AllGamesGetCourtAndStartTime()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c", "d"]);
        var courts = new List<string> { "c1", "c2" };

        GameScheduler.Schedule(games, courts, StartTime, GameLength);

        games.Should().AllSatisfy(g =>
        {
            g.CourtId.Should().NotBeNull();
            g.StartTime.Should().NotBeNull();
        });
    }

    [Fact]
    public void Schedule_NoTeamInTwoGamesAtSameTime()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c", "d"]);
        var courts = new List<string> { "c1", "c2" };

        GameScheduler.Schedule(games, courts, StartTime, GameLength);

        AssertNoSameSlotConflicts(games);
    }

    [Fact]
    public void Schedule_ThreeTeams_ConsecutiveSlots()
    {
        // 3-team group: games should be in consecutive slots (no unnecessary gaps)
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c"]);
        var courts = new List<string> { "c1" };

        GameScheduler.Schedule(games, courts, StartTime, GameLength);

        var times = games.Select(g => g.StartTime!.Value).OrderBy(t => t).ToList();
        for (var i = 1; i < times.Count; i++)
        {
            (times[i] - times[i - 1]).TotalMinutes
                .Should().Be(GameLength, "3-team games should be scheduled consecutively");
        }
    }

    [Fact]
    public void Schedule_CourtsUniformlyDistributed()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c", "d"]);
        var courts = new List<string> { "c1", "c2", "c3" };

        GameScheduler.Schedule(games, courts, StartTime, GameLength);

        var courtCounts = games.GroupBy(g => g.CourtId).Select(g => g.Count()).ToList();
        var maxDiff = courtCounts.Max() - courtCounts.Min();
        maxDiff.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void Schedule_GroupsInterleaved()
    {
        var gamesA = RoundRobinGenerator.Generate("t1", "p1", "gA", ["a1", "a2", "a3"]);
        var gamesB = RoundRobinGenerator.Generate("t1", "p1", "gB", ["b1", "b2", "b3"]);
        var allGames = gamesA.Concat(gamesB).ToList();
        var courts = new List<string> { "c1", "c2" };

        GameScheduler.Schedule(allGames, courts, StartTime, GameLength);

        // Check that games from both groups are interleaved
        var orderedByTime = allGames.OrderBy(g => g.StartTime).ToList();

        // First two games (in first time slots) should be from different groups
        var firstSlotGames = orderedByTime
            .Where(g => g.StartTime == orderedByTime[0].StartTime)
            .ToList();

        if (firstSlotGames.Count >= 2)
        {
            firstSlotGames.Select(g => g.GroupId).Distinct().Count()
                .Should().Be(2, "first time slot should have games from both groups");
        }
    }

    [Fact]
    public void Schedule_SingleCourt_AllGamesSequential()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c"]);
        var courts = new List<string> { "c1" };

        GameScheduler.Schedule(games, courts, StartTime, GameLength);

        games.Should().AllSatisfy(g => g.CourtId.Should().Be("c1"));

        var times = games.Select(g => g.StartTime!.Value).OrderBy(t => t).ToList();
        for (var i = 1; i < times.Count; i++)
        {
            (times[i] - times[i - 1]).TotalMinutes.Should().BeGreaterThanOrEqualTo(GameLength);
        }
    }

    [Fact]
    public void Schedule_PlayoffRoundsInOrder()
    {
        var games = PlayoffEliminationGenerator.Generate("t1", "p1", "g1",
            ["s1", "s2", "s3", "s4"]);
        var courts = new List<string> { "c1", "c2" };

        GameScheduler.Schedule(games, courts, StartTime, GameLength);

        var round1MaxTime = games.Where(g => g.Round == 1).Max(g => g.StartTime!.Value);
        var round2MinTime = games.Where(g => g.Round == 2).Min(g => g.StartTime!.Value);

        round2MinTime.Should().BeOnOrAfter(round1MaxTime);
    }

    [Fact]
    public void Schedule_NoConsecutiveRefereeing()
    {
        // 4-team group: every game has a referee, ensure no team referees back-to-back
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c", "d"]);
        var courts = new List<string> { "c1" };

        GameScheduler.Schedule(games, courts, StartTime, GameLength);

        var ordered = games.OrderBy(g => g.StartTime).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            var prev = ordered[i - 1];
            var curr = ordered[i];

            // Only check truly consecutive slots (no gap)
            if (curr.StartTime!.Value - prev.StartTime!.Value != TimeSpan.FromMinutes(GameLength))
                continue;

            if (prev.RefereeTeamId is not null && curr.RefereeTeamId is not null)
            {
                curr.RefereeTeamId.Should().NotBe(prev.RefereeTeamId,
                    $"team {prev.RefereeTeamId} should not referee consecutive slots " +
                    $"at {prev.StartTime} and {curr.StartTime}");
            }
        }
    }

    [Fact]
    public void Schedule_UsesCorrectGameLength()
    {
        // Use independent teams so no back-to-back constraint applies
        var games = new List<Game>
        {
            Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b"),
            Game.Create("t1", "p1", "g2", 1, homeTeamId: "c", awayTeamId: "d")
        };
        var courts = new List<string> { "c1" };

        GameScheduler.Schedule(games, courts, StartTime, 45);

        var times = games.OrderBy(g => g.StartTime).Select(g => g.StartTime!.Value).ToList();
        (times[1] - times[0]).TotalMinutes.Should().Be(45);
    }

    private static void AssertNoSameSlotConflicts(List<Game> games)
    {
        var scheduled = games.Where(g => g.StartTime.HasValue).ToList();

        // Group by start time (same slot)
        foreach (var slotGroup in scheduled.GroupBy(g => g.StartTime!.Value))
        {
            var gamesInSlot = slotGroup.ToList();
            if (gamesInSlot.Count <= 1) continue;

            // Collect all involved teams per game in this slot
            var allTeams = new HashSet<string>();
            foreach (var game in gamesInSlot)
            {
                var involved = new List<string?> { game.HomeTeamId, game.AwayTeamId, game.RefereeTeamId };
                foreach (var teamId in involved.Where(t => t is not null))
                {
                    allTeams.Contains(teamId!).Should().BeFalse(
                        $"team {teamId} appears in multiple games at {slotGroup.Key}");
                    allTeams.Add(teamId!);
                }
            }
        }
    }
}
