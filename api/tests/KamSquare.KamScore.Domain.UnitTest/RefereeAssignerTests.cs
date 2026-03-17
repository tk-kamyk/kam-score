using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Domain.UnitTest;

public class RefereeAssignerTests
{
    private static readonly DateTime StartTime = new(2026, 6, 1, 9, 0, 0);
    private const int GameLength = 30;

    [Fact]
    public void Assign_EmptyGames_ShouldNotThrow()
    {
        var act = () => RefereeAssigner.Assign([], GameLength);
        act.Should().NotThrow();
    }

    [Fact]
    public void Assign_UnscheduledGamesAreSkipped()
    {
        var scheduled = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c"]);
        GameScheduler.Schedule(scheduled, ["c1"], ["g1"], StartTime, GameLength);

        // Add an unscheduled game (no StartTime)
        var unscheduled = Game.Create("t1", "p1", "g1", 99, homeTeamId: "a", awayTeamId: "b");
        var allGames = scheduled.Concat([unscheduled]).ToList();

        RefereeAssigner.Assign(allGames, GameLength);

        unscheduled.RefereeTeamId.Should().BeNull("unscheduled games should be skipped");
        scheduled.Should().AllSatisfy(g => g.RefereeTeamId.Should().NotBeNull());
    }

    [Fact]
    public void Assign_TwoTeams_NoRefereeAvailable()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b"]);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        games.Should().AllSatisfy(g => g.RefereeTeamId.Should().BeNull(
            "only 2 teams, no one available to referee"));
    }

    [Fact]
    public void Assign_ThreeTeams_AllGamesHaveReferee()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c"]);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        games.Should().AllSatisfy(g => g.RefereeTeamId.Should().NotBeNull());
    }

    [Fact]
    public void Assign_FourTeams_SingleCourt_SomeGamesHaveReferee()
    {
        // With 4 teams and 1 court, adjacent slots use all teams so some can't get referees
        var teams = new List<string> { "a", "b", "c", "d" };
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", teams);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        var gamesWithReferee = games.Where(g => g.RefereeTeamId is not null).ToList();
        gamesWithReferee.Should().HaveCountGreaterThanOrEqualTo(2,
            "with 4 teams and 1 court, at least 2 of 6 games should get referees");
        gamesWithReferee.Should().AllSatisfy(g =>
            teams.Should().Contain(g.RefereeTeamId!));
    }

    [Fact]
    public void Assign_FiveTeams_SingleCourt_AllGamesHaveReferee()
    {
        // With 5 teams and 1 court, enough free teams for full referee coverage
        var teams = new List<string> { "a", "b", "c", "d", "e" };
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", teams);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        games.Should().AllSatisfy(g => g.RefereeTeamId.Should().NotBeNull(
            "with 5 teams and 1 court, there's always a free team to referee"));
    }

    [Fact]
    public void Assign_SixTeams_SingleCourt_AllGamesHaveReferee()
    {
        var teams = new List<string> { "a", "b", "c", "d", "e", "f" };
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", teams);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        games.Should().AllSatisfy(g => g.RefereeTeamId.Should().NotBeNull(
            "with 6 teams and 1 court, there's always a free team to referee"));
    }

    [Fact]
    public void Assign_RefereeIsNeverPlayingInSameGame()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c", "d", "e"]);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        foreach (var game in games.Where(g => g.RefereeTeamId is not null))
        {
            game.RefereeTeamId.Should().NotBe(game.HomeTeamId);
            game.RefereeTeamId.Should().NotBe(game.AwayTeamId);
        }
    }

    [Fact]
    public void Assign_RefereeNotBusyInSameSlot()
    {
        // Use 2 courts with enough teams so some slots have parallel games
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1",
            ["a", "b", "c", "d", "e", "f"]);
        GameScheduler.Schedule(games, ["c1", "c2"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        foreach (var slotGroup in games.Where(g => g.StartTime.HasValue).GroupBy(g => g.StartTime!.Value))
        {
            var gamesInSlot = slotGroup.ToList();
            if (gamesInSlot.Count <= 1) continue;

            foreach (var game in gamesInSlot.Where(g => g.RefereeTeamId is not null))
            {
                var otherGames = gamesInSlot.Where(g => g != game);
                foreach (var other in otherGames)
                {
                    game.RefereeTeamId.Should().NotBe(other.HomeTeamId,
                        $"referee {game.RefereeTeamId} is playing in another game at {game.StartTime}");
                    game.RefereeTeamId.Should().NotBe(other.AwayTeamId,
                        $"referee {game.RefereeTeamId} is playing in another game at {game.StartTime}");
                }
            }
        }
    }

    [Fact]
    public void Assign_NoConsecutiveRefereeing()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c", "d", "e"]);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        var ordered = games.OrderBy(g => g.StartTime).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            var prev = ordered[i - 1];
            var curr = ordered[i];

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
    public void Assign_RefereeDoesNotPlayInNextSlot()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", ["a", "b", "c", "d", "e"]);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        AssertRefereeDoesNotPlayInNextSlot(games);
    }

    [Fact]
    public void Assign_FiveTeams_BalancedDistribution()
    {
        var teams = new List<string> { "a", "b", "c", "d", "e" };
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", teams);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        // 10 games, 5 teams → 2 referee duties each on average
        foreach (var team in teams)
        {
            var refCount = games.Count(g => g.RefereeTeamId == team);
            refCount.Should().BeGreaterThanOrEqualTo(1, $"team {team} should referee at least once");
            refCount.Should().BeLessThanOrEqualTo(3, $"team {team} should referee at most 3 times");
        }
    }

    [Fact]
    public void Assign_SixTeams_BalancedDistribution()
    {
        var teams = new List<string> { "a", "b", "c", "d", "e", "f" };
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1", teams);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        // 15 games, 6 teams → ~2-3 referee duties each
        games.Should().HaveCount(15);
        foreach (var team in teams)
        {
            var refCount = games.Count(g => g.RefereeTeamId == team);
            refCount.Should().BeGreaterThanOrEqualTo(1, $"team {team} should referee at least once");
            refCount.Should().BeLessThanOrEqualTo(4, $"team {team} should referee at most 4 times");
        }
    }

    [Fact]
    public void Assign_MultiGroup_RefereesFromOwnGroup()
    {
        var gamesA = RoundRobinGenerator.Generate("t1", "p1", "gA", ["a1", "a2", "a3"]);
        var gamesB = RoundRobinGenerator.Generate("t1", "p1", "gB", ["b1", "b2", "b3"]);
        var allGames = gamesA.Concat(gamesB).ToList();
        GameScheduler.Schedule(allGames, ["c1", "c2"], ["gA", "gB"], StartTime, GameLength);

        RefereeAssigner.Assign(allGames, GameLength);

        var groupATeams = new HashSet<string> { "a1", "a2", "a3" };
        var groupBTeams = new HashSet<string> { "b1", "b2", "b3" };

        foreach (var game in allGames.Where(g => g.RefereeTeamId is not null))
        {
            if (game.GroupId == "gA")
                groupATeams.Should().Contain(game.RefereeTeamId!,
                    "referee for group A game should be from group A");
            else
                groupBTeams.Should().Contain(game.RefereeTeamId!,
                    "referee for group B game should be from group B");
        }
    }

    [Fact]
    public void Assign_MultipleCourts_SomeGamesGetReferees()
    {
        // With multiple courts, fewer free teams per slot — but some should still get referees
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1",
            ["a", "b", "c", "d", "e", "f"]);
        GameScheduler.Schedule(games, ["c1", "c2"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        var gamesWithReferee = games.Count(g => g.RefereeTeamId is not null);
        gamesWithReferee.Should().BeGreaterThan(0, "some games should get referees");

        // All assigned referees should respect constraints
        AssertRefereeDoesNotPlayInNextSlot(games);
    }

    [Fact]
    public void Assign_SixTeams_RefereeDoesNotPlayInNextSlot()
    {
        var games = RoundRobinGenerator.Generate("t1", "p1", "g1",
            ["a", "b", "c", "d", "e", "f"]);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        AssertRefereeDoesNotPlayInNextSlot(games);
    }

    private static void AssertRefereeDoesNotPlayInNextSlot(List<Game> games)
    {
        var scheduled = games.Where(g => g.StartTime.HasValue).ToList();

        // Build map of which teams play in each time slot
        var playingInSlot = new Dictionary<DateTime, HashSet<string>>();
        foreach (var game in scheduled)
        {
            var time = game.StartTime!.Value;
            if (!playingInSlot.ContainsKey(time))
                playingInSlot[time] = [];
            if (game.HomeTeamId is not null) playingInSlot[time].Add(game.HomeTeamId);
            if (game.AwayTeamId is not null) playingInSlot[time].Add(game.AwayTeamId);
        }

        foreach (var game in scheduled.Where(g => g.RefereeTeamId is not null))
        {
            var nextSlotTime = game.StartTime!.Value.AddMinutes(GameLength);
            if (playingInSlot.TryGetValue(nextSlotTime, out var nextPlaying))
            {
                nextPlaying.Should().NotContain(game.RefereeTeamId!,
                    $"team {game.RefereeTeamId} referees at {game.StartTime} but plays at {nextSlotTime}");
            }
        }
    }
}
