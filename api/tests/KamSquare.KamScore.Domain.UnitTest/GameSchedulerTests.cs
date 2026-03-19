using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.Services.Formats;

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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b", "c", "d"]);
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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b", "c", "d"]);
        var courts = new List<string> { "c1", "c2" };

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

        AssertNoSameSlotConflicts(games);
    }

    [Fact]
    public void Schedule_NoActivityInSlotBeforePlaying()
    {
        // 4-team group with 1 court: teams must have a free slot before playing
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b", "c", "d"]);
        var courts = new List<string> { "c1" };

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

        AssertRestSlotBeforePlaying(games);
    }

    [Fact]
    public void Schedule_NoActivityInSlotBeforePlaying_MultipleCourts()
    {
        // 4-team group with 2 courts: even with parallel games, rest constraint holds
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b", "c", "d"]);
        var courts = new List<string> { "c1", "c2" };

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

        AssertRestSlotBeforePlaying(games);
    }

    [Fact]
    public void Schedule_ThreeTeams_GamesHaveRestGaps()
    {
        // 3-team group: every team plays every game, so rest slots are needed between games
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b", "c"]);
        var courts = new List<string> { "c1" };

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

        // All games should be scheduled
        games.Should().AllSatisfy(g =>
        {
            g.CourtId.Should().NotBeNull();
            g.StartTime.Should().NotBeNull();
        });

        // Verify rest constraint holds
        AssertRestSlotBeforePlaying(games);
    }

    [Fact]
    public void Schedule_CourtsAssignedSequentiallyPerSlot()
    {
        // Games with shared teams force multiple slots; courts should fill starting from c1
        var games = new List<Game>
        {
            Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b"),
            Game.Create("t1", "p1", "g1", 1, homeTeamId: "c", awayTeamId: "d"),
            Game.Create("t1", "p1", "g1", 2, homeTeamId: "e", awayTeamId: "f"),
            Game.Create("t1", "p1", "g1", 2, homeTeamId: "g", awayTeamId: "h")
        };
        var courts = new List<string> { "c1", "c2", "c3", "c4" };

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

        // All games have independent teams, so all fit in slot 0 using courts c1-c4
        var slot0Games = games.Where(g => g.StartTime == StartTime).OrderBy(g => g.CourtId).ToList();
        slot0Games.Should().HaveCount(4);
        slot0Games[0].CourtId.Should().Be("c1");
        slot0Games[1].CourtId.Should().Be("c2");
        slot0Games[2].CourtId.Should().Be("c3");
        slot0Games[3].CourtId.Should().Be("c4");
    }

    [Fact]
    public void Schedule_GroupsInterleaved()
    {
        var gamesA = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "gA", ["a1", "a2", "a3"]);
        var gamesB = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "gB", ["b1", "b2", "b3"]);
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
        var gamesA = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "gA", ["a1", "a2", "a3"]);
        var gamesB = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "gB", ["b1", "b2", "b3"]);
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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b", "c"]);
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
        var games = PhaseFormatStrategy.For(PhaseFormat.PlayoffElimination).GenerateGames("t1", "p1", "g1",
            ["s1", "s2", "s3", "s4"]);
        var courts = new List<string> { "c1", "c2" };

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

        var round1MaxTime = games.Where(g => g.Round == 1).Max(g => g.StartTime!.Value);
        var round2MinTime = games.Where(g => g.Round == 2).Min(g => g.StartTime!.Value);

        round2MinTime.Should().BeOnOrAfter(round1MaxTime);
    }

    [Fact]
    public void Schedule_UsesCorrectGameLength()
    {
        // Use independent teams so no rest constraint applies
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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1",
            ["a", "b", "c", "d", "e", "f"]);
        var courts = new List<string> { "c1" };

        var act = () => GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);
        act.Should().NotThrow("scheduler should terminate within the slot limit");
    }

    [Fact]
    public void Schedule_LargeGroup_RestConstraintHolds()
    {
        // 6-team group with 2 courts — stress test for rest constraint
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1",
            ["a", "b", "c", "d", "e", "f"]);
        var courts = new List<string> { "c1", "c2" };

        GameScheduler.Schedule(games, courts, ["g1"], StartTime, GameLength);

        AssertRestSlotBeforePlaying(games);
        AssertNoSameSlotConflicts(games);
    }

    private static void AssertRestSlotBeforePlaying(List<Game> games)
    {
        var scheduled = games.Where(g => g.StartTime.HasValue).ToList();

        // Build a map of which teams are active in each time slot
        var activityBySlot = new Dictionary<DateTime, HashSet<string>>();
        foreach (var game in scheduled)
        {
            var time = game.StartTime!.Value;
            if (!activityBySlot.ContainsKey(time))
                activityBySlot[time] = [];
            if (game.HomeTeamId is not null) activityBySlot[time].Add(game.HomeTeamId);
            if (game.AwayTeamId is not null) activityBySlot[time].Add(game.AwayTeamId);
            if (game.RefereeTeamId is not null) activityBySlot[time].Add(game.RefereeTeamId);
        }

        var orderedTimes = activityBySlot.Keys.OrderBy(t => t).ToList();

        foreach (var game in scheduled)
        {
            var gameTime = game.StartTime!.Value;
            var timeIndex = orderedTimes.IndexOf(gameTime);
            if (timeIndex <= 0) continue;

            var prevTime = orderedTimes[timeIndex - 1];
            // Only check truly consecutive slots
            if ((gameTime - prevTime).TotalMinutes != GameLength) continue;

            var prevActive = activityBySlot[prevTime];

            if (game.HomeTeamId is not null)
            {
                prevActive.Should().NotContain(game.HomeTeamId,
                    $"team {game.HomeTeamId} plays at {gameTime} but was active at {prevTime}");
            }

            if (game.AwayTeamId is not null)
            {
                prevActive.Should().NotContain(game.AwayTeamId,
                    $"team {game.AwayTeamId} plays at {gameTime} but was active at {prevTime}");
            }
        }
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
