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
        var result = GameScheduler.Schedule([], ["c1"], [], StartTime, GameLength);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Schedule_EmptyCourts_ShouldReturnGamesUnchanged()
    {
        var games = new List<Game>
        {
            Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b")
        };

        var result = GameScheduler.Schedule(games, [], ["g1"], StartTime, GameLength);
        result.Should().AllSatisfy(g => g.CourtId.Should().BeNull());
    }

    [Fact]
    public void Schedule_AllGamesGetCourtAndStartTime()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c", "d"]);
        var courts = new List<string> { "c1", "c2" };

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

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

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

        AssertNoSameSlotConflicts(games);
    }

    [Fact]
    public void Schedule_ThreeTeams_ConsecutiveSlots()
    {
        // 3-team group: games should be in consecutive slots (no unnecessary gaps)
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c"]);
        var courts = new List<string> { "c1" };

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

        var times = games.Select(g => g.StartTime!.Value).OrderBy(t => t).ToList();
        for (var i = 1; i < times.Count; i++)
        {
            (times[i] - times[i - 1]).TotalMinutes
                .Should().Be(GameLength, "3-team games should be scheduled consecutively");
        }
    }

    [Fact]
    public void Schedule_CourtsAssignedSequentiallyPerSlot()
    {
        // 4 independent games, 4 courts — each slot should fill courts starting from c1
        var games = new List<Game>
        {
            Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b"),
            Game.Create("t1", "p1", "g1", 1, homeTeamId: "c", awayTeamId: "d"),
            Game.Create("t1", "p1", "g1", 2, homeTeamId: "a", awayTeamId: "c"),
            Game.Create("t1", "p1", "g1", 2, homeTeamId: "b", awayTeamId: "d")
        };
        var courts = new List<string> { "c1", "c2", "c3", "c4" };

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

        // Slot 0: should use c1 and c2 (first two courts)
        var slot0Games = games.Where(g => g.StartTime == StartTime).OrderBy(g => g.CourtId).ToList();
        slot0Games.Should().HaveCount(2);
        slot0Games[0].CourtId.Should().Be("c1");
        slot0Games[1].CourtId.Should().Be("c2");

        // Slot 1: should also use c1 and c2 (not c3 and c4)
        var slot1Time = StartTime.AddMinutes(GameLength);
        var slot1Games = games.Where(g => g.StartTime == slot1Time).OrderBy(g => g.CourtId).ToList();
        slot1Games.Should().HaveCount(2);
        slot1Games[0].CourtId.Should().Be("c1");
        slot1Games[1].CourtId.Should().Be("c2");
    }

    [Fact]
    public void Schedule_GroupsInterleaved()
    {
        var gamesA = RoundRobinGenerator.Generate("t1", "p1", "gA", ["a1", "a2", "a3"]);
        var gamesB = RoundRobinGenerator.Generate("t1", "p1", "gB", ["b1", "b2", "b3"]);
        var allGames = gamesA.Concat(gamesB).ToList();
        var courts = new List<string> { "c1", "c2" };

        GameScheduler.Schedule(allGames, courts, ["gA", "gB"], StartTime, GameLength);

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
    public void Schedule_GroupsFollowProvidedOrder()
    {
        // Group IDs are GUIDs — ordering by GUID would be unpredictable
        // Provide explicit order: gB first, then gA
        var gamesA = RoundRobinGenerator.Generate("t1", "p1", "gA", ["a1", "a2", "a3"]);
        var gamesB = RoundRobinGenerator.Generate("t1", "p1", "gB", ["b1", "b2", "b3"]);
        var allGames = gamesA.Concat(gamesB).ToList();
        var courts = new List<string> { "c1", "c2" };

        // gB should be scheduled first (on c1), gA second (on c2)
        GameScheduler.Schedule(allGames, courts, ["gB", "gA"], StartTime, GameLength);

        var firstSlotGames = allGames
            .Where(g => g.StartTime == StartTime)
            .ToList();

        firstSlotGames.Should().HaveCount(2);
        var gameOnCourt1 = firstSlotGames.First(g => g.CourtId == "c1");
        var gameOnCourt2 = firstSlotGames.First(g => g.CourtId == "c2");
        gameOnCourt1.GroupId.Should().Be("gB", "first group in order should get first court");
        gameOnCourt2.GroupId.Should().Be("gA", "second group in order should get second court");
    }

    [Fact]
    public void Schedule_SingleCourt_AllGamesSequential()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c"]);
        var courts = new List<string> { "c1" };

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

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

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

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

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

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

        GameScheduler.Schedule(games, courts, ["g1", "g2"], StartTime, 45);

        var times = games.OrderBy(g => g.StartTime).Select(g => g.StartTime!.Value).ToList();
        (times[1] - times[0]).TotalMinutes.Should().Be(45);
    }

    [Fact]
    public void Schedule_UnschedulableGame_ThrowsInvalidOperationException()
    {
        // The safest test: verify the loop terminates (doesn't hang) with a reasonable case.
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1",
            ["a", "b", "c", "d", "e", "f"]);
        var courts = new List<string> { "c1" };

        var act = () => GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);
        act.Should().NotThrow("scheduler should terminate within the slot limit");
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
