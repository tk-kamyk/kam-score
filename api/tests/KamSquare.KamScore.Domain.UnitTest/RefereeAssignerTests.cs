using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.Services.Formats;

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
        var scheduled = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b", "c"]);
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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b"]);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        games.Should().AllSatisfy(g => g.RefereeTeamId.Should().BeNull(
            "only 2 teams, no one available to referee"));
    }

    [Fact]
    public void Assign_ThreeTeams_AllGamesHaveReferee()
    {
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b", "c"]);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        games.Should().AllSatisfy(g => g.RefereeTeamId.Should().NotBeNull());
    }

    [Fact]
    public void Assign_FourTeams_SingleCourt_SomeGamesHaveReferee()
    {
        // With 4 teams and 1 court, adjacent slots use all teams so some can't get referees
        var teams = new List<string> { "a", "b", "c", "d" };
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", teams);
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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", teams);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        games.Should().AllSatisfy(g => g.RefereeTeamId.Should().NotBeNull(
            "with 5 teams and 1 court, there's always a free team to referee"));
    }

    [Fact]
    public void Assign_SixTeams_SingleCourt_AllGamesHaveReferee()
    {
        var teams = new List<string> { "a", "b", "c", "d", "e", "f" };
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", teams);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        games.Should().AllSatisfy(g => g.RefereeTeamId.Should().NotBeNull(
            "with 6 teams and 1 court, there's always a free team to referee"));
    }

    [Fact]
    public void Assign_RefereeIsNeverPlayingInSameGame()
    {
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b", "c", "d", "e"]);
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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1",
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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b", "c", "d", "e"]);
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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", ["a", "b", "c", "d", "e"]);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        AssertRefereeDoesNotPlayInNextSlot(games);
    }

    [Fact]
    public void Assign_FiveTeams_BalancedDistribution()
    {
        var teams = new List<string> { "a", "b", "c", "d", "e" };
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", teams);
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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1", teams);
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
        var gamesA = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "gA", ["a1", "a2", "a3"]);
        var gamesB = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "gB", ["b1", "b2", "b3"]);
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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1",
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
        var games = PhaseFormatStrategy.For(PhaseFormat.RoundRobin).GenerateGames("t1", "p1", "g1",
            ["a", "b", "c", "d", "e", "f"]);
        GameScheduler.Schedule(games, ["c1"], ["g1"], StartTime, GameLength);

        RefereeAssigner.Assign(games, GameLength);

        AssertRefereeDoesNotPlayInNextSlot(games);
    }

    // --- GetCandidates tests ---

    [Fact]
    public void GetCandidates_ReturnsTeamsFromSameLevelAcrossGroups()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", "level1", ["a1", "a2", "a3"]),
            CreateGroup("gB", "level1", ["b1", "b2", "b3"]),
            CreateGroup("gC", "level2", ["c1", "c2", "c3"]),
        };

        var targetGame = Game.Create("t1", "p1", "gA", 1, homeTeamId: "a1", awayTeamId: "a2");
        targetGame.AssignSchedule("c1", StartTime);

        var otherGame = Game.Create("t1", "p1", "gB", 1, homeTeamId: "b1", awayTeamId: "b2");
        otherGame.AssignSchedule("c2", StartTime.AddMinutes(GameLength * 2)); // different slot

        var allGames = new List<Game> { targetGame, otherGame };

        var candidates = RefereeAssigner.GetCandidates(targetGame, allGames, groups, GameLength);

        // Should include a3 (same group, free), b1, b2, b3 (same level, free)
        candidates.Should().Contain("a3");
        candidates.Should().Contain("b1");
        candidates.Should().Contain("b2");
        candidates.Should().Contain("b3");
        // Should NOT include c1, c2, c3 (different level)
        candidates.Should().NotContain("c1");
        candidates.Should().NotContain("c2");
        candidates.Should().NotContain("c3");
    }

    [Fact]
    public void GetCandidates_NoLevels_ReturnsTeamsFromAllGroups()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["a1", "a2", "a3"]),
            CreateGroup("gB", null, ["b1", "b2", "b3"]),
        };

        var targetGame = Game.Create("t1", "p1", "gA", 1, homeTeamId: "a1", awayTeamId: "a2");
        targetGame.AssignSchedule("c1", StartTime);

        var allGames = new List<Game> { targetGame };

        var candidates = RefereeAssigner.GetCandidates(targetGame, allGames, groups, GameLength);

        candidates.Should().Contain("a3");
        candidates.Should().Contain("b1");
        candidates.Should().Contain("b2");
        candidates.Should().Contain("b3");
    }

    [Fact]
    public void GetCandidates_ExcludesTeamsPlayingInSameSlot()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["a1", "a2", "a3"]),
            CreateGroup("gB", null, ["b1", "b2", "b3"]),
        };

        var targetGame = Game.Create("t1", "p1", "gA", 1, homeTeamId: "a1", awayTeamId: "a2");
        targetGame.AssignSchedule("c1", StartTime);

        var sameSlotGame = Game.Create("t1", "p1", "gB", 1, homeTeamId: "b1", awayTeamId: "b2");
        sameSlotGame.AssignSchedule("c2", StartTime); // same time slot

        var allGames = new List<Game> { targetGame, sameSlotGame };

        var candidates = RefereeAssigner.GetCandidates(targetGame, allGames, groups, GameLength);

        candidates.Should().NotContain("a1");
        candidates.Should().NotContain("a2");
        candidates.Should().NotContain("b1");
        candidates.Should().NotContain("b2");
        candidates.Should().Contain("a3");
        candidates.Should().Contain("b3");
    }

    [Fact]
    public void GetCandidates_ExcludesTeamsRefereeingInSameSlot()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["a1", "a2", "a3", "a4"]),
        };

        var targetGame = Game.Create("t1", "p1", "gA", 1, homeTeamId: "a1", awayTeamId: "a2");
        targetGame.AssignSchedule("c1", StartTime);

        var sameSlotGame = Game.Create("t1", "p1", "gA", 1, homeTeamId: "a3", awayTeamId: "a4",
            refereeTeamId: "a3"); // a3 is marked as referee (wrong, but to simulate busy)
        sameSlotGame.AssignSchedule("c2", StartTime);
        sameSlotGame.RefereeTeamId = "a3"; // explicitly assigned referee in same slot

        var allGames = new List<Game> { targetGame, sameSlotGame };

        var candidates = RefereeAssigner.GetCandidates(targetGame, allGames, groups, GameLength);

        // a3 is playing in same slot, a4 is playing in same slot
        candidates.Should().NotContain("a3");
        candidates.Should().NotContain("a4");
    }

    [Fact]
    public void GetCandidates_ExcludesTeamsPlayingInNextSlot()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["a1", "a2", "a3", "a4"]),
        };

        var targetGame = Game.Create("t1", "p1", "gA", 1, homeTeamId: "a1", awayTeamId: "a2");
        targetGame.AssignSchedule("c1", StartTime);

        var nextSlotGame = Game.Create("t1", "p1", "gA", 2, homeTeamId: "a3", awayTeamId: "a4");
        nextSlotGame.AssignSchedule("c1", StartTime.AddMinutes(GameLength)); // next slot

        var allGames = new List<Game> { targetGame, nextSlotGame };

        var candidates = RefereeAssigner.GetCandidates(targetGame, allGames, groups, GameLength);

        candidates.Should().NotContain("a1");
        candidates.Should().NotContain("a2");
        candidates.Should().NotContain("a3", "a3 plays in the next slot");
        candidates.Should().NotContain("a4", "a4 plays in the next slot");
    }

    [Fact]
    public void GetCandidates_ExcludesHomeAndAwayTeams()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["a1", "a2", "a3"]),
        };

        var targetGame = Game.Create("t1", "p1", "gA", 1, homeTeamId: "a1", awayTeamId: "a2");
        targetGame.AssignSchedule("c1", StartTime);

        var candidates = RefereeAssigner.GetCandidates(targetGame, [targetGame], groups, GameLength);

        candidates.Should().NotContain("a1");
        candidates.Should().NotContain("a2");
        candidates.Should().Contain("a3");
    }

    [Fact]
    public void GetCandidates_IncludesPlaceholderTeams()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["a1", "a2", "placeholder1"]),
        };

        var targetGame = Game.Create("t1", "p1", "gA", 1, homeTeamId: "a1", awayTeamId: "a2");
        targetGame.AssignSchedule("c1", StartTime);

        var candidates = RefereeAssigner.GetCandidates(targetGame, [targetGame], groups, GameLength);

        candidates.Should().Contain("placeholder1");
    }

    // --- GetCandidates: Bracket Placeholder Tests ---

    [Fact]
    public void GetCandidates_EliminationSfGame_IncludesPlaceholdersFromQfRound()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8"]),
        };

        // QF games (round 1) — all scheduled earlier
        var qf1 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t1", awayTeamId: "t2", label: "QF1");
        qf1.AssignSchedule("c1", StartTime);
        var qf2 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t3", awayTeamId: "t4", label: "QF2");
        qf2.AssignSchedule("c2", StartTime);
        var qf3 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t5", awayTeamId: "t6", label: "QF3");
        qf3.AssignSchedule("c1", StartTime.AddMinutes(GameLength));
        var qf4 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t7", awayTeamId: "t8", label: "QF4");
        qf4.AssignSchedule("c2", StartTime.AddMinutes(GameLength));

        // SF1 game (round 2) — target game with placeholders
        var sf1 = Game.Create("t1", "p1", "gA", 2,
            homeTeamPlaceholder: "Winner QF1", awayTeamPlaceholder: "Winner QF2", label: "SF1");
        sf1.AssignSchedule("c1", StartTime.AddMinutes(GameLength * 2));

        var allGames = new List<Game> { qf1, qf2, qf3, qf4, sf1 };

        var candidates = RefereeAssigner.GetCandidates(sf1, allGames, groups, GameLength);

        // Should include losers from QF1 and QF2 (and winners/losers from QF3, QF4 which are in earlier slots)
        candidates.Should().Contain("Loser QF1");
        candidates.Should().Contain("Loser QF2");
        candidates.Should().Contain("Winner QF3");
        candidates.Should().Contain("Loser QF3");
        candidates.Should().Contain("Winner QF4");
        candidates.Should().Contain("Loser QF4");
    }

    [Fact]
    public void GetCandidates_EliminationSfGame_ExcludesPlaceholdersPlayingInTargetGame()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["t1", "t2", "t3", "t4"]),
        };

        var qf1 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t1", awayTeamId: "t2", label: "QF1");
        qf1.AssignSchedule("c1", StartTime);
        var qf2 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t3", awayTeamId: "t4", label: "QF2");
        qf2.AssignSchedule("c2", StartTime);

        var sf1 = Game.Create("t1", "p1", "gA", 2,
            homeTeamPlaceholder: "Winner QF1", awayTeamPlaceholder: "Winner QF2", label: "SF1");
        sf1.AssignSchedule("c1", StartTime.AddMinutes(GameLength));

        var allGames = new List<Game> { qf1, qf2, sf1 };

        var candidates = RefereeAssigner.GetCandidates(sf1, allGames, groups, GameLength);

        candidates.Should().NotContain("Winner QF1", "playing as home in target game");
        candidates.Should().NotContain("Winner QF2", "playing as away in target game");
    }

    [Fact]
    public void GetCandidates_EliminationSfGame_ExcludesPlaceholderBusyInSameSlot()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8"]),
        };

        var qf1 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t1", awayTeamId: "t2", label: "QF1");
        qf1.AssignSchedule("c1", StartTime);
        var qf2 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t3", awayTeamId: "t4", label: "QF2");
        qf2.AssignSchedule("c2", StartTime);
        var qf3 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t5", awayTeamId: "t6", label: "QF3");
        qf3.AssignSchedule("c1", StartTime.AddMinutes(GameLength));
        var qf4 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t7", awayTeamId: "t8", label: "QF4");
        qf4.AssignSchedule("c2", StartTime.AddMinutes(GameLength));

        // SF1 and SF2 in the same time slot
        var sf1 = Game.Create("t1", "p1", "gA", 2,
            homeTeamPlaceholder: "Winner QF1", awayTeamPlaceholder: "Winner QF2", label: "SF1");
        sf1.AssignSchedule("c1", StartTime.AddMinutes(GameLength * 2));

        var sf2 = Game.Create("t1", "p1", "gA", 2,
            homeTeamPlaceholder: "Winner QF3", awayTeamPlaceholder: "Winner QF4", label: "SF2");
        sf2.AssignSchedule("c2", StartTime.AddMinutes(GameLength * 2));

        var allGames = new List<Game> { qf1, qf2, qf3, qf4, sf1, sf2 };

        var candidates = RefereeAssigner.GetCandidates(sf1, allGames, groups, GameLength);

        // Winner QF3 and Winner QF4 are playing in SF2 at the same time slot
        candidates.Should().NotContain("Winner QF3", "playing in SF2 in the same slot");
        candidates.Should().NotContain("Winner QF4", "playing in SF2 in the same slot");
    }

    [Fact]
    public void GetCandidates_EliminationFinal_IncludesPlaceholdersFromAllEarlierRounds()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["t1", "t2", "t3", "t4"]),
        };

        var sf1 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t1", awayTeamId: "t2", label: "SF1");
        sf1.AssignSchedule("c1", StartTime);
        var sf2 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t3", awayTeamId: "t4", label: "SF2");
        sf2.AssignSchedule("c2", StartTime);

        var final_ = Game.Create("t1", "p1", "gA", 2,
            homeTeamPlaceholder: "Winner SF1", awayTeamPlaceholder: "Winner SF2", label: "Final");
        final_.AssignSchedule("c1", StartTime.AddMinutes(GameLength));

        var allGames = new List<Game> { sf1, sf2, final_ };

        var candidates = RefereeAssigner.GetCandidates(final_, allGames, groups, GameLength);

        // Should include losers from SF round
        candidates.Should().Contain("Loser SF1");
        candidates.Should().Contain("Loser SF2");
    }

    [Fact]
    public void GetCandidates_EliminationPlaceholderPlayingInNextSlot_IsExcluded()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8"]),
        };

        var qf1 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t1", awayTeamId: "t2", label: "QF1");
        qf1.AssignSchedule("c1", StartTime);
        var qf2 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t3", awayTeamId: "t4", label: "QF2");
        qf2.AssignSchedule("c2", StartTime);

        // SF1 is in slot 1
        var sf1 = Game.Create("t1", "p1", "gA", 2,
            homeTeamPlaceholder: "Winner QF1", awayTeamPlaceholder: "Winner QF2", label: "SF1");
        sf1.AssignSchedule("c1", StartTime.AddMinutes(GameLength));

        // Final is in slot 2 — "Winner SF1" plays here
        var final_ = Game.Create("t1", "p1", "gA", 3,
            homeTeamPlaceholder: "Winner SF1", awayTeamPlaceholder: "Winner SF2", label: "Final");
        final_.AssignSchedule("c1", StartTime.AddMinutes(GameLength * 2));

        var allGames = new List<Game> { qf1, qf2, sf1, final_ };

        var candidates = RefereeAssigner.GetCandidates(sf1, allGames, groups, GameLength);

        // "Winner SF1" plays in the next slot (Final), so excluded
        candidates.Should().NotContain("Winner SF1", "plays in the next time slot");
    }

    [Fact]
    public void GetCandidates_EliminationSf2_IncludesPlaceholdersFromSf1WhenInDifferentSlot()
    {
        var groups = new List<Group>
        {
            CreateGroup("gA", null, ["t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8"]),
        };

        var qf1 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t1", awayTeamId: "t2", label: "QF1");
        qf1.AssignSchedule("c1", StartTime);
        var qf2 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t3", awayTeamId: "t4", label: "QF2");
        qf2.AssignSchedule("c2", StartTime);
        var qf3 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t5", awayTeamId: "t6", label: "QF3");
        qf3.AssignSchedule("c1", StartTime.AddMinutes(GameLength));
        var qf4 = Game.Create("t1", "p1", "gA", 1, homeTeamId: "t7", awayTeamId: "t8", label: "QF4");
        qf4.AssignSchedule("c2", StartTime.AddMinutes(GameLength));

        // SF1 at slot 2, SF2 at slot 3 (different time slots, single court)
        var sf1 = Game.Create("t1", "p1", "gA", 2,
            homeTeamPlaceholder: "Winner QF1", awayTeamPlaceholder: "Winner QF2", label: "SF1");
        sf1.AssignSchedule("c1", StartTime.AddMinutes(GameLength * 2));

        var sf2 = Game.Create("t1", "p1", "gA", 2,
            homeTeamPlaceholder: "Winner QF3", awayTeamPlaceholder: "Winner QF4", label: "SF2");
        sf2.AssignSchedule("c1", StartTime.AddMinutes(GameLength * 3));

        var allGames = new List<Game> { qf1, qf2, qf3, qf4, sf1, sf2 };

        var candidates = RefereeAssigner.GetCandidates(sf2, allGames, groups, GameLength);

        // SF2 should see SF1 placeholders since SF1 is in a different (earlier) slot
        candidates.Should().Contain("Loser SF1", "SF1 loser is free during SF2");
        candidates.Should().Contain("Winner SF1", "SF1 winner is free during SF2");
    }

    private static Group CreateGroup(string id, string? levelId, List<string> teamIds)
    {
        var group = Group.Create("Group", levelId);
        group.Id = id;
        foreach (var teamId in teamIds)
            group.AddTeam(teamId);
        return group;
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
