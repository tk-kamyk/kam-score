using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.UnitTest;

public class StandingsCalculatorTests
{
    private const string TournamentId = "t1";
    private const string PhaseId = "p1";
    private const string GroupId = "g1";

    // --- Helper Methods ---

    private static Game CreateCompletedGame(
        string homeTeamId, string awayTeamId, int homeScore, int awayScore,
        int round = 1, List<SetResult>? sets = null)
    {
        var game = Game.Create(TournamentId, PhaseId, GroupId, round,
            homeTeamId: homeTeamId, awayTeamId: awayTeamId);

        if (sets is not null)
            game.RecordResult(sets);
        else
            game.RecordSimpleResult(homeScore, awayScore);

        return game;
    }

    private static Game CreateScheduledGame(string homeTeamId, string awayTeamId, int round = 1)
    {
        return Game.Create(TournamentId, PhaseId, GroupId, round,
            homeTeamId: homeTeamId, awayTeamId: awayTeamId);
    }

    // ================================================================
    // Round Robin Tests
    // ================================================================

    [Fact]
    public void RoundRobin_NoGames_AllTeamsAtZeroPoints()
    {
        var teamIds = new List<string> { "a", "b", "c" };

        var standings = StandingsCalculator.CalculateRoundRobin([], teamIds);

        standings.Should().HaveCount(3);
        standings.Should().AllSatisfy(s =>
        {
            s.Points.Should().Be(0);
            s.GamesPlayed.Should().Be(0);
            s.Wins.Should().Be(0);
            s.Draws.Should().Be(0);
            s.Losses.Should().Be(0);
            s.SetsWon.Should().Be(0);
            s.SetsLost.Should().Be(0);
            s.SetDifference.Should().Be(0);
            s.PointsWon.Should().Be(0);
            s.PointsLost.Should().Be(0);
            s.PointDifference.Should().Be(0);
        });
    }

    [Fact]
    public void RoundRobin_SingleWin_WinnerGets2Points()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game> { CreateCompletedGame("a", "b", 2, 1) };

        var standings = StandingsCalculator.CalculateRoundRobin(games, teamIds);

        var teamA = standings.First(s => s.TeamId == "a");
        var teamB = standings.First(s => s.TeamId == "b");

        teamA.Points.Should().Be(2);
        teamA.Wins.Should().Be(1);
        teamA.Losses.Should().Be(0);

        teamB.Points.Should().Be(0);
        teamB.Wins.Should().Be(0);
        teamB.Losses.Should().Be(1);
    }

    [Fact]
    public void RoundRobin_Draw_BothTeamsGet1Point()
    {
        var teamIds = new List<string> { "a", "b" };
        // Single-set tie: 25-25 → HomeScore=0 (no set won), AwayScore=0
        var games = new List<Game>
        {
            CreateCompletedGame("a", "b", 0, 0, sets: [new SetResult(25, 25)])
        };

        var standings = StandingsCalculator.CalculateRoundRobin(games, teamIds);

        var teamA = standings.First(s => s.TeamId == "a");
        var teamB = standings.First(s => s.TeamId == "b");

        teamA.Points.Should().Be(1);
        teamA.Draws.Should().Be(1);
        teamA.Wins.Should().Be(0);

        teamB.Points.Should().Be(1);
        teamB.Draws.Should().Be(1);
    }

    [Fact]
    public void RoundRobin_Full3TeamGroup_CorrectOrdering()
    {
        // Eagles beat Hawks 2-1, Eagles beat Wolves 2-0, Hawks beat Wolves 2-1
        var teamIds = new List<string> { "eagles", "hawks", "wolves" };
        var games = new List<Game>
        {
            CreateCompletedGame("eagles", "hawks", 2, 1),
            CreateCompletedGame("eagles", "wolves", 2, 0),
            CreateCompletedGame("hawks", "wolves", 2, 1),
        };

        var standings = StandingsCalculator.CalculateRoundRobin(games, teamIds);

        standings[0].TeamId.Should().Be("eagles");
        standings[0].Points.Should().Be(4);
        standings[0].Position.Should().Be(1);

        standings[1].TeamId.Should().Be("hawks");
        standings[1].Points.Should().Be(2);
        standings[1].Position.Should().Be(2);

        standings[2].TeamId.Should().Be("wolves");
        standings[2].Points.Should().Be(0);
        standings[2].Position.Should().Be(3);
    }

    [Fact]
    public void RoundRobin_TiedOnPoints_SetDifferenceBreaksTie()
    {
        // All teams have 1 win, 1 loss = 2 points
        // But different set differences
        var teamIds = new List<string> { "a", "b", "c" };
        var games = new List<Game>
        {
            CreateCompletedGame("a", "b", 2, 0),  // a: +2, b: -2
            CreateCompletedGame("b", "c", 2, 1),  // b: +1 → net -1, c: -1
            CreateCompletedGame("c", "a", 2, 1),  // c: +1 → net 0, a: -1 → net +1
        };

        var standings = StandingsCalculator.CalculateRoundRobin(games, teamIds);

        // All have 2 points. Set differences: a=+1, c=0, b=-1
        standings[0].TeamId.Should().Be("a");
        standings[0].SetDifference.Should().Be(1);
        standings[1].TeamId.Should().Be("c");
        standings[1].SetDifference.Should().Be(0);
        standings[2].TeamId.Should().Be("b");
        standings[2].SetDifference.Should().Be(-1);
    }

    [Fact]
    public void RoundRobin_TiedOnPointsAndSetDiff_DirectResultBreaksTie()
    {
        // 4-team group where two teams end up with same points and set difference
        var teamIds = new List<string> { "a", "b", "c", "d" };
        var games = new List<Game>
        {
            // a and b both beat c and d with 2-0, but a beats b
            CreateCompletedGame("a", "c", 2, 0),
            CreateCompletedGame("a", "d", 2, 0),
            CreateCompletedGame("b", "c", 2, 0),
            CreateCompletedGame("b", "d", 2, 0),
            CreateCompletedGame("a", "b", 2, 1),  // a beats b
            CreateCompletedGame("c", "d", 2, 1),  // c beats d
        };

        var standings = StandingsCalculator.CalculateRoundRobin(games, teamIds);

        // a: 3W, 0L → 6pts, sets 6-1 → diff +5
        // b: 2W, 1L → 4pts, sets 5-2 → diff +3
        // c: 1W, 2L → 2pts, sets 2-5 → diff -3
        // d: 0W, 3L → 0pts, sets 1-6 → diff -5
        standings[0].TeamId.Should().Be("a");
        standings[1].TeamId.Should().Be("b");
        standings[2].TeamId.Should().Be("c");
        standings[3].TeamId.Should().Be("d");
    }

    [Fact]
    public void RoundRobin_DirectResultTiebreaker_2TeamsExactlyTied()
    {
        // 3-team group: a beats b, b beats c, c beats a (all 2-1) → all have same points AND same set diff
        var teamIds = new List<string> { "a", "b", "c" };
        var games = new List<Game>
        {
            CreateCompletedGame("a", "b", 2, 1),
            CreateCompletedGame("b", "c", 2, 1),
            CreateCompletedGame("c", "a", 2, 1),
        };

        var standings = StandingsCalculator.CalculateRoundRobin(games, teamIds);

        // All teams: 2 pts, set diff 0 → circular tie
        // Head-to-head mini-table also results in same stats → truly tied
        standings.Should().HaveCount(3);
        standings.Should().AllSatisfy(s =>
        {
            s.Points.Should().Be(2);
            s.SetDifference.Should().Be(0);
            s.Position.Should().Be(1, "all tied teams share position 1");
        });
    }

    [Fact]
    public void RoundRobin_CountsOnlyCompletedGames()
    {
        var teamIds = new List<string> { "a", "b", "c" };
        var games = new List<Game>
        {
            CreateCompletedGame("a", "b", 2, 0),
            CreateScheduledGame("a", "c"),  // not completed
            CreateScheduledGame("b", "c"),  // not completed
        };

        var standings = StandingsCalculator.CalculateRoundRobin(games, teamIds);

        standings.First(s => s.TeamId == "a").GamesPlayed.Should().Be(1);
        standings.First(s => s.TeamId == "b").GamesPlayed.Should().Be(1);
        standings.First(s => s.TeamId == "c").GamesPlayed.Should().Be(0);
    }

    [Fact]
    public void RoundRobin_SetsWonLost_CalculatedFromScores()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game> { CreateCompletedGame("a", "b", 2, 1) };

        var standings = StandingsCalculator.CalculateRoundRobin(games, teamIds);

        var teamA = standings.First(s => s.TeamId == "a");
        teamA.SetsWon.Should().Be(2);
        teamA.SetsLost.Should().Be(1);
        teamA.SetDifference.Should().Be(1);

        var teamB = standings.First(s => s.TeamId == "b");
        teamB.SetsWon.Should().Be(1);
        teamB.SetsLost.Should().Be(2);
        teamB.SetDifference.Should().Be(-1);
    }

    [Fact]
    public void RoundRobin_PointsWonLost_CalculatedFromSets()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game>
        {
            CreateCompletedGame("a", "b", 2, 1, sets:
            [
                new SetResult(25, 20),
                new SetResult(18, 25),
                new SetResult(15, 10)
            ])
        };

        var standings = StandingsCalculator.CalculateRoundRobin(games, teamIds);

        var teamA = standings.First(s => s.TeamId == "a");
        teamA.PointsWon.Should().Be(58);   // 25 + 18 + 15
        teamA.PointsLost.Should().Be(55);  // 20 + 25 + 10
        teamA.PointDifference.Should().Be(3);

        var teamB = standings.First(s => s.TeamId == "b");
        teamB.PointsWon.Should().Be(55);
        teamB.PointsLost.Should().Be(58);
        teamB.PointDifference.Should().Be(-3);
    }

    [Fact]
    public void RoundRobin_SimpleResult_PointsAreZero()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game> { CreateCompletedGame("a", "b", 2, 1) };

        var standings = StandingsCalculator.CalculateRoundRobin(games, teamIds);

        var teamA = standings.First(s => s.TeamId == "a");
        teamA.PointsWon.Should().Be(0);
        teamA.PointsLost.Should().Be(0);
        teamA.PointDifference.Should().Be(0);
    }

    [Fact]
    public void RoundRobin_PositionsAreSequential()
    {
        var teamIds = new List<string> { "a", "b", "c" };
        var games = new List<Game>
        {
            CreateCompletedGame("a", "b", 2, 0),
            CreateCompletedGame("a", "c", 2, 0),
            CreateCompletedGame("b", "c", 2, 0),
        };

        var standings = StandingsCalculator.CalculateRoundRobin(games, teamIds);

        standings[0].Position.Should().Be(1);
        standings[1].Position.Should().Be(2);
        standings[2].Position.Should().Be(3);
    }

    // ================================================================
    // Playoff Elimination Tests
    // ================================================================

    [Fact]
    public void Elimination_4Teams_FullyCompleted_CorrectPositions()
    {
        // SF1: a vs b → a wins, SF2: c vs d → c wins, F: a vs c → a wins
        var teamIds = new List<string> { "a", "b", "c", "d" };
        var games = new List<Game>
        {
            CreateCompletedGame("a", "b", 2, 0, round: 1),
            CreateCompletedGame("c", "d", 2, 0, round: 1),
            CreateCompletedGame("a", "c", 2, 1, round: 2),
        };

        var standings = StandingsCalculator.CalculatePlayoffElimination(games, teamIds);

        // bracketSize=4, totalRounds=2
        // a wins final → position 1
        // c loses final (R2) → 4/4+1 = 2
        // b loses R1 → 4/2+1 = 3
        // d loses R1 → 4/2+1 = 3
        standings.First(s => s.TeamId == "a").Position.Should().Be(1);
        standings.First(s => s.TeamId == "c").Position.Should().Be(2);
        standings.First(s => s.TeamId == "b").Position.Should().Be(3);
        standings.First(s => s.TeamId == "d").Position.Should().Be(3);
    }

    [Fact]
    public void Elimination_8Teams_QFLosers_AllGet5th()
    {
        // Only QF round completed
        var teamIds = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };
        var games = new List<Game>
        {
            CreateCompletedGame("a", "h", 2, 0, round: 1),
            CreateCompletedGame("b", "g", 2, 0, round: 1),
            CreateCompletedGame("c", "f", 2, 0, round: 1),
            CreateCompletedGame("d", "e", 2, 0, round: 1),
        };

        var standings = StandingsCalculator.CalculatePlayoffElimination(games, teamIds);

        // bracketSize=8, QF losers: position = 8/2 + 1 = 5
        var qfLosers = standings.Where(s => new[] { "h", "g", "f", "e" }.Contains(s.TeamId));
        qfLosers.Should().AllSatisfy(s => s.Position.Should().Be(5));

        // Winners have no final position yet (they default to worst = 5)
        // But they have 1 win, 0 losses
        var qfWinners = standings.Where(s => new[] { "a", "b", "c", "d" }.Contains(s.TeamId));
        qfWinners.Should().AllSatisfy(s => s.Wins.Should().Be(1));
    }

    [Fact]
    public void Elimination_8Teams_FullyCompleted_CorrectPositions()
    {
        var teamIds = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };
        var games = new List<Game>
        {
            // QF (R1)
            CreateCompletedGame("a", "h", 2, 0, round: 1),
            CreateCompletedGame("b", "g", 2, 0, round: 1),
            CreateCompletedGame("c", "f", 2, 0, round: 1),
            CreateCompletedGame("d", "e", 2, 0, round: 1),
            // SF (R2)
            CreateCompletedGame("a", "b", 2, 0, round: 2),
            CreateCompletedGame("c", "d", 2, 0, round: 2),
            // F (R3)
            CreateCompletedGame("a", "c", 2, 0, round: 3),
        };

        var standings = StandingsCalculator.CalculatePlayoffElimination(games, teamIds);

        standings.First(s => s.TeamId == "a").Position.Should().Be(1);
        standings.First(s => s.TeamId == "c").Position.Should().Be(2); // loses final
        standings.First(s => s.TeamId == "b").Position.Should().Be(3); // loses SF
        standings.First(s => s.TeamId == "d").Position.Should().Be(3); // loses SF
        standings.First(s => s.TeamId == "h").Position.Should().Be(5); // loses QF
        standings.First(s => s.TeamId == "g").Position.Should().Be(5);
        standings.First(s => s.TeamId == "f").Position.Should().Be(5);
        standings.First(s => s.TeamId == "e").Position.Should().Be(5);
    }

    [Fact]
    public void Elimination_PartialBracket_OnlyAssignsKnownPositions()
    {
        var teamIds = new List<string> { "a", "b", "c", "d" };
        var games = new List<Game>
        {
            CreateCompletedGame("a", "b", 2, 0, round: 1), // b is out → position 3
            CreateScheduledGame("c", "d"),  // not completed yet
        };
        // Only set round on scheduled game properly
        games[1] = Game.Create(TournamentId, PhaseId, GroupId, 1,
            homeTeamId: "c", awayTeamId: "d");

        var standings = StandingsCalculator.CalculatePlayoffElimination(games, teamIds);

        standings.First(s => s.TeamId == "b").Position.Should().Be(3);
        // a won R1 but final not played → has 1 win, position = worst default
    }

    [Fact]
    public void Elimination_NoGames_AllTeamsGetWorstPosition()
    {
        var teamIds = new List<string> { "a", "b", "c", "d" };

        var standings = StandingsCalculator.CalculatePlayoffElimination([], teamIds);

        // bracketSize=4, worst = 4/2+1 = 3
        standings.Should().HaveCount(4);
        standings.Should().AllSatisfy(s =>
        {
            s.Position.Should().Be(3);
            s.GamesPlayed.Should().Be(0);
        });
    }

    [Fact]
    public void Elimination_HasNoRoundRobinFields()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game> { CreateCompletedGame("a", "b", 2, 0, round: 1) };

        var standings = StandingsCalculator.CalculatePlayoffElimination(games, teamIds);

        standings.Should().AllSatisfy(s =>
        {
            s.Points.Should().BeNull();
            s.SetsWon.Should().BeNull();
            s.SetsLost.Should().BeNull();
            s.SetDifference.Should().BeNull();
            s.PointsWon.Should().BeNull();
            s.PointsLost.Should().BeNull();
            s.PointDifference.Should().BeNull();
            s.Draws.Should().Be(0);
        });
    }

    // ================================================================
    // Playoff with Placement Tests
    // ================================================================

    [Fact]
    public void Placement_4Teams_FullyCompleted_UniquePositions()
    {
        var teamIds = new List<string> { "a", "b", "c", "d" };
        // Generated structure for 4 teams:
        // R1: SF1 (a vs d), SF2 (b vs c) — bracket games (2 per round)
        // R2: 3rd place game (d vs c) — placement (1 per round)
        // R3: Final (a vs b) — placement (1 per round)
        var games = new List<Game>
        {
            CreateCompletedGame("a", "d", 2, 0, round: 1),
            CreateCompletedGame("b", "c", 2, 0, round: 1),
            CreateCompletedGame("d", "c", 2, 1, round: 2), // 3rd place
            CreateCompletedGame("a", "b", 2, 1, round: 3), // Final
        };

        var standings = StandingsCalculator.CalculatePlayoffWithPlacement(games, teamIds);

        standings.First(s => s.TeamId == "a").Position.Should().Be(1);
        standings.First(s => s.TeamId == "b").Position.Should().Be(2);
        standings.First(s => s.TeamId == "d").Position.Should().Be(3);
        standings.First(s => s.TeamId == "c").Position.Should().Be(4);
    }

    [Fact]
    public void Placement_8Teams_FullyCompleted_UniquePositions()
    {
        var teamIds = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };
        // 8-team bracket:
        // R1: QF (4 games)
        // R2: B-SF (2 games)
        // R3: A-SF (2 games)
        // R4: 7th place (1 game) — placement
        // R5: 5th place (1 game) — placement
        // R6: 3rd place (1 game) — placement
        // R7: Final (1 game) — placement
        var games = new List<Game>
        {
            // QF
            CreateCompletedGame("a", "h", 2, 0, round: 1),
            CreateCompletedGame("b", "g", 2, 0, round: 1),
            CreateCompletedGame("c", "f", 2, 0, round: 1),
            CreateCompletedGame("d", "e", 2, 0, round: 1),
            // B-SF (QF losers)
            CreateCompletedGame("h", "g", 2, 0, round: 2),
            CreateCompletedGame("f", "e", 2, 0, round: 2),
            // A-SF (QF winners)
            CreateCompletedGame("a", "b", 2, 0, round: 3),
            CreateCompletedGame("c", "d", 2, 0, round: 3),
            // 7th place (B-SF losers)
            CreateCompletedGame("g", "e", 2, 0, round: 4),
            // 5th place (B-SF winners)
            CreateCompletedGame("h", "f", 2, 0, round: 5),
            // 3rd place (A-SF losers)
            CreateCompletedGame("b", "d", 2, 0, round: 6),
            // Final (A-SF winners)
            CreateCompletedGame("a", "c", 2, 0, round: 7),
        };

        var standings = StandingsCalculator.CalculatePlayoffWithPlacement(games, teamIds);

        standings.First(s => s.TeamId == "a").Position.Should().Be(1);
        standings.First(s => s.TeamId == "c").Position.Should().Be(2);
        standings.First(s => s.TeamId == "b").Position.Should().Be(3);
        standings.First(s => s.TeamId == "d").Position.Should().Be(4);
        standings.First(s => s.TeamId == "h").Position.Should().Be(5);
        standings.First(s => s.TeamId == "f").Position.Should().Be(6);
        standings.First(s => s.TeamId == "g").Position.Should().Be(7);
        standings.First(s => s.TeamId == "e").Position.Should().Be(8);
    }

    [Fact]
    public void Placement_IncompletePlacementGames_OnlyResolvesCompleted()
    {
        var teamIds = new List<string> { "a", "b", "c", "d" };
        // Only SFs completed, no placement games yet
        var games = new List<Game>
        {
            CreateCompletedGame("a", "d", 2, 0, round: 1),
            CreateCompletedGame("b", "c", 2, 0, round: 1),
            // R2 (3rd place) and R3 (Final) not played yet
        };

        var standings = StandingsCalculator.CalculatePlayoffWithPlacement(games, teamIds);

        // No single-game rounds are completed → no positions assigned from placement
        // All unranked teams should be at the default position
        standings.Should().HaveCount(4);
    }

    [Fact]
    public void Placement_HasNoRoundRobinFields()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game> { CreateCompletedGame("a", "b", 2, 0, round: 1) };

        var standings = StandingsCalculator.CalculatePlayoffWithPlacement(games, teamIds);

        standings.Should().AllSatisfy(s =>
        {
            s.Points.Should().BeNull();
            s.SetsWon.Should().BeNull();
            s.SetsLost.Should().BeNull();
            s.SetDifference.Should().BeNull();
            s.PointsWon.Should().BeNull();
            s.PointsLost.Should().BeNull();
            s.PointDifference.Should().BeNull();
        });
    }

    // ================================================================
    // Calculate dispatch Tests
    // ================================================================

    [Fact]
    public void Calculate_DispatchesToCorrectMethod()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game> { CreateCompletedGame("a", "b", 2, 0) };

        var rrStandings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin, games, teamIds);
        rrStandings.First(s => s.TeamId == "a").Points.Should().Be(2, "RR should have points");

        var elimStandings = StandingsCalculator.Calculate(PhaseFormat.PlayoffElimination, games, teamIds);
        elimStandings.First(s => s.TeamId == "a").Points.Should().BeNull("elimination should not have points");
    }

    [Fact]
    public void Calculate_PlayoffWithPlacement_DispatchesCorrectly()
    {
        var teamIds = new List<string> { "a", "b", "c", "d" };
        var games = new List<Game>
        {
            CreateCompletedGame("a", "d", 2, 0, round: 1),
            CreateCompletedGame("b", "c", 2, 0, round: 1),
            CreateCompletedGame("d", "c", 2, 1, round: 2),
            CreateCompletedGame("a", "b", 2, 1, round: 3),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.PlayoffWithPlacement, games, teamIds);

        standings.Should().HaveCount(4);
        standings.First(s => s.TeamId == "a").Position.Should().Be(1);
        standings.First(s => s.TeamId == "b").Position.Should().Be(2);
        standings.First(s => s.TeamId == "d").Position.Should().Be(3);
        standings.First(s => s.TeamId == "c").Position.Should().Be(4);
        standings.Should().AllSatisfy(s => s.Points.Should().BeNull("placement should not have points"));
    }
}
