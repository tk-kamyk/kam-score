using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
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

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,[], teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

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
    public void RoundRobin_TiedOnPointsAndSetDiff_PointDifferenceBreaksTie()
    {
        // 3-team circular: all 2 pts, all set diff 0
        // Different point differences from set details
        var teamIds = new List<string> { "a", "b", "c" };
        var games = new List<Game>
        {
            // a beats b 2-1: a scores 60, b scores 55 → margin +5
            CreateCompletedGame("a", "b", 2, 1, sets:
            [
                new SetResult(25, 20), new SetResult(20, 25), new SetResult(15, 10)
            ]),
            // b beats c 2-1: b scores 60, c scores 55 → margin +5
            CreateCompletedGame("b", "c", 2, 1, sets:
            [
                new SetResult(25, 20), new SetResult(20, 25), new SetResult(15, 10)
            ]),
            // c beats a 2-1: c scores 63, a scores 61 → margin +2
            CreateCompletedGame("c", "a", 2, 1, sets:
            [
                new SetResult(25, 23), new SetResult(23, 25), new SetResult(15, 13)
            ]),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

        // All: 2 pts, set diff 0
        // Point diffs: a=+3 (121-118), b=0 (115-115), c=-3 (118-121)
        standings[0].TeamId.Should().Be("a");
        standings[0].PointDifference.Should().Be(3);
        standings[1].TeamId.Should().Be("b");
        standings[1].PointDifference.Should().Be(0);
        standings[2].TeamId.Should().Be("c");
        standings[2].PointDifference.Should().Be(-3);
    }

    [Fact]
    public void RoundRobin_PointsScoredBreaksTie_AfterCircularDirectResult()
    {
        // 3-team circular: all 2 pts, set diff 0, point diff 0
        // Circular direct result → still tied after h2h
        // Different total points scored breaks the tie
        var teamIds = new List<string> { "a", "b", "c" };
        var games = new List<Game>
        {
            // a beats b 2-1: a scores 75, b scores 70 (margin +5)
            CreateCompletedGame("a", "b", 2, 1, sets:
            [
                new SetResult(25, 22), new SetResult(22, 25), new SetResult(28, 23)
            ]),
            // b beats c 2-1: b scores 65, c scores 60 (margin +5)
            CreateCompletedGame("b", "c", 2, 1, sets:
            [
                new SetResult(25, 20), new SetResult(20, 25), new SetResult(20, 15)
            ]),
            // c beats a 2-1: c scores 55, a scores 50 (margin +5)
            CreateCompletedGame("c", "a", 2, 1, sets:
            [
                new SetResult(20, 15), new SetResult(15, 20), new SetResult(20, 15)
            ]),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

        // All: 2 pts, set diff 0, point diff 0
        // Points scored: b=135, a=125, c=115
        standings.Should().AllSatisfy(s =>
        {
            s.Points.Should().Be(2);
            s.SetDifference.Should().Be(0);
            s.PointDifference.Should().Be(0);
        });

        standings[0].TeamId.Should().Be("b");
        standings[0].PointsWon.Should().Be(135);
        standings[1].TeamId.Should().Be("a");
        standings[1].PointsWon.Should().Be(125);
        standings[2].TeamId.Should().Be("c");
        standings[2].PointsWon.Should().Be(115);
    }

    [Fact]
    public void RoundRobin_DirectResultTiebreaker_CircularTie_AllSharePosition()
    {
        // 3-team group: a beats b, b beats c, c beats a (all 2-1, simple results)
        // No set details → point diff 0, points scored 0 → truly tied
        var teamIds = new List<string> { "a", "b", "c" };
        var games = new List<Game>
        {
            CreateCompletedGame("a", "b", 2, 1),
            CreateCompletedGame("b", "c", 2, 1),
            CreateCompletedGame("c", "a", 2, 1),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

        standings.First(s => s.TeamId == "a").GamesPlayed.Should().Be(1);
        standings.First(s => s.TeamId == "b").GamesPlayed.Should().Be(1);
        standings.First(s => s.TeamId == "c").GamesPlayed.Should().Be(0);
    }

    [Fact]
    public void RoundRobin_SetsWonLost_CalculatedFromScores()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game> { CreateCompletedGame("a", "b", 2, 1) };

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.RoundRobin,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.PlayoffElimination,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.PlayoffElimination,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.PlayoffElimination,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.PlayoffElimination,games, teamIds);

        standings.First(s => s.TeamId == "b").Position.Should().Be(3);
        // a won R1 but final not played → has 1 win, position = worst default
    }

    [Fact]
    public void Elimination_NoGames_AllTeamsGetWorstPosition()
    {
        var teamIds = new List<string> { "a", "b", "c", "d" };

        var standings = StandingsCalculator.Calculate(PhaseFormat.PlayoffElimination,[], teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.PlayoffElimination,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.PlayoffWithPlacement,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.PlayoffWithPlacement,games, teamIds);

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

        var standings = StandingsCalculator.Calculate(PhaseFormat.PlayoffWithPlacement,games, teamIds);

        // No single-game rounds are completed → no positions assigned from placement
        // All unranked teams should be at the default position
        standings.Should().HaveCount(4);
    }

    [Fact]
    public void Placement_HasNoRoundRobinFields()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game> { CreateCompletedGame("a", "b", 2, 0, round: 1) };

        var standings = StandingsCalculator.Calculate(PhaseFormat.PlayoffWithPlacement,games, teamIds);

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
    // Double Elimination Tests
    // ================================================================

    private static Game CreateLabeledCompletedGame(
        string homeTeamId, string awayTeamId, int homeScore, int awayScore,
        int round, string label)
    {
        var game = Game.Create(TournamentId, PhaseId, GroupId, round,
            homeTeamId: homeTeamId, awayTeamId: awayTeamId, label: label);
        game.RecordSimpleResult(homeScore, awayScore);
        return game;
    }

    [Fact]
    public void DoubleElimination_4Teams_FullyCompleted_CorrectPositions()
    {
        // WB: SF1(a vs d → a wins), SF2(b vs c → b wins), WB-F(a vs b → a wins)
        // LB: LB-R1(loser d vs loser c → d wins), LB-R2(d vs loser b → d wins)
        // GF: a vs d → a wins
        var teamIds = new List<string> { "a", "b", "c", "d" };
        var games = new List<Game>
        {
            CreateLabeledCompletedGame("a", "d", 2, 0, round: 1, "WB-SF1"),
            CreateLabeledCompletedGame("b", "c", 2, 0, round: 1, "WB-SF2"),
            CreateLabeledCompletedGame("a", "b", 2, 0, round: 2, "WB-Final"),
            CreateLabeledCompletedGame("d", "c", 2, 0, round: 3, "LB-R1-1"),
            CreateLabeledCompletedGame("d", "b", 2, 0, round: 4, "LB-R2-1"),
            CreateLabeledCompletedGame("a", "d", 2, 0, round: 5, "Grand Final"),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.DoubleElimination,games, teamIds);

        standings.First(s => s.TeamId == "a").Position.Should().Be(1); // GF winner
        standings.First(s => s.TeamId == "d").Position.Should().Be(2); // GF loser
        standings.First(s => s.TeamId == "b").Position.Should().Be(3); // lost in LB-R2
        standings.First(s => s.TeamId == "c").Position.Should().Be(4); // lost in LB-R1
    }

    [Fact]
    public void DoubleElimination_NoGamesCompleted_AllAtWorstPosition()
    {
        var teamIds = new List<string> { "a", "b", "c", "d" };
        var games = new List<Game>();

        var standings = StandingsCalculator.Calculate(PhaseFormat.DoubleElimination,games, teamIds);

        standings.Should().HaveCount(4);
        standings.Should().AllSatisfy(s => s.Position.Should().Be(4));
    }

    [Fact]
    public void DoubleElimination_HasNoRoundRobinFields()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game>
        {
            CreateLabeledCompletedGame("a", "b", 2, 0, round: 1, "Grand Final"),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.DoubleElimination,games, teamIds);

        standings.Should().AllSatisfy(s =>
        {
            s.Points.Should().BeNull();
            s.SetsWon.Should().BeNull();
            s.SetsLost.Should().BeNull();
            s.SetDifference.Should().BeNull();
        });
    }

    [Fact]
    public void DoubleElimination_GfWinnerIsFirst_GfLoserIsSecond()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game>
        {
            CreateLabeledCompletedGame("a", "b", 2, 1, round: 1, "Grand Final"),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.DoubleElimination,games, teamIds);

        standings.First(s => s.TeamId == "a").Position.Should().Be(1);
        standings.First(s => s.TeamId == "b").Position.Should().Be(2);
    }

    [Fact]
    public void DoubleElimination_DispatchesCorrectly()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game>
        {
            CreateLabeledCompletedGame("a", "b", 2, 0, round: 1, "Grand Final"),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.DoubleElimination, games, teamIds);

        standings.Should().HaveCount(2);
        standings.First(s => s.TeamId == "a").Position.Should().Be(1);
        standings.First(s => s.TeamId == "b").Position.Should().Be(2);
        standings.Should().AllSatisfy(s => s.Points.Should().BeNull());
    }

    // ================================================================
    // Double Elimination VD Tests
    // ================================================================

    [Fact]
    public void DoubleEliminationVd_FullyCompleted_CorrectPositions()
    {
        var teamIds = Enumerable.Range(1, 8).Select(i => $"t{i}").ToList();
        var games = new List<Game>
        {
            // R1: QFs
            CreateLabeledCompletedGame("t1", "t8", 2, 0, round: 1, "QF1"),
            CreateLabeledCompletedGame("t4", "t5", 2, 0, round: 1, "QF2"),
            CreateLabeledCompletedGame("t2", "t7", 2, 0, round: 1, "QF3"),
            CreateLabeledCompletedGame("t3", "t6", 2, 0, round: 1, "QF4"),
            // R2: Winners
            CreateLabeledCompletedGame("t1", "t4", 2, 0, round: 2, "W1"),
            CreateLabeledCompletedGame("t2", "t3", 2, 0, round: 2, "W2"),
            // R3: Losers
            CreateLabeledCompletedGame("t8", "t5", 2, 0, round: 3, "L1"),
            CreateLabeledCompletedGame("t7", "t6", 2, 0, round: 3, "L2"),
            // R4: Crossover (cross-bracket: LW2 vs WL1, LW1 vs WL2)
            CreateLabeledCompletedGame("t3", "t8", 2, 0, round: 4, "X1"),
            CreateLabeledCompletedGame("t4", "t7", 2, 0, round: 4, "X2"),
            // R5: Grand SFs
            CreateLabeledCompletedGame("t1", "t3", 2, 0, round: 5, "GSF1"),
            CreateLabeledCompletedGame("t2", "t4", 2, 0, round: 5, "GSF2"),
            // R6: 7th Place
            CreateLabeledCompletedGame("t5", "t6", 2, 0, round: 6, "7th Place"),
            // R7: Grand Final
            CreateLabeledCompletedGame("t1", "t2", 2, 0, round: 7, "Grand Final"),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.DoubleEliminationVd,games, teamIds);

        standings.First(s => s.TeamId == "t1").Position.Should().Be(1); // GF winner
        standings.First(s => s.TeamId == "t2").Position.Should().Be(2); // GF loser
        standings.First(s => s.TeamId == "t3").Position.Should().Be(3); // GSF loser
        standings.First(s => s.TeamId == "t4").Position.Should().Be(3); // GSF loser (shared)
        standings.First(s => s.TeamId == "t8").Position.Should().Be(5); // X loser
        standings.First(s => s.TeamId == "t7").Position.Should().Be(5); // X loser (shared)
        standings.First(s => s.TeamId == "t5").Position.Should().Be(7); // 7th Place winner
        standings.First(s => s.TeamId == "t6").Position.Should().Be(8); // 7th Place loser
    }

    [Fact]
    public void DoubleEliminationVd_NoGamesCompleted_AllAtWorstPosition()
    {
        var teamIds = Enumerable.Range(1, 8).Select(i => $"t{i}").ToList();

        var standings = StandingsCalculator.Calculate(PhaseFormat.DoubleEliminationVd,[], teamIds);

        standings.Should().HaveCount(8);
        standings.Should().AllSatisfy(s => s.Position.Should().Be(8));
    }

    [Fact]
    public void DoubleEliminationVd_GfWinnerFirst_GfLoserSecond()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game>
        {
            CreateLabeledCompletedGame("a", "b", 2, 1, round: 7, "Grand Final"),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.DoubleEliminationVd,games, teamIds);

        standings.First(s => s.TeamId == "a").Position.Should().Be(1);
        standings.First(s => s.TeamId == "b").Position.Should().Be(2);
    }

    [Fact]
    public void DoubleEliminationVd_HasNoRoundRobinFields()
    {
        var teamIds = Enumerable.Range(1, 8).Select(i => $"t{i}").ToList();
        var games = new List<Game>
        {
            CreateLabeledCompletedGame("t1", "t2", 2, 0, round: 7, "Grand Final"),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.DoubleEliminationVd,games, teamIds);

        standings.Should().AllSatisfy(s =>
        {
            s.Points.Should().BeNull();
            s.SetsWon.Should().BeNull();
            s.SetsLost.Should().BeNull();
            s.SetDifference.Should().BeNull();
        });
    }

    [Fact]
    public void DoubleEliminationVd_DispatchesCorrectly()
    {
        var teamIds = new List<string> { "a", "b" };
        var games = new List<Game>
        {
            CreateLabeledCompletedGame("a", "b", 2, 0, round: 7, "Grand Final"),
        };

        var standings = StandingsCalculator.Calculate(PhaseFormat.DoubleEliminationVd, games, teamIds);

        standings.Should().HaveCount(2);
        standings.First(s => s.TeamId == "a").Position.Should().Be(1);
        standings.First(s => s.TeamId == "b").Position.Should().Be(2);
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

    // ================================================================
    // Phase.CalculateAllGroupStandings Tests
    // ================================================================

    [Fact]
    public void Phase_CalculateAllGroupStandings_ReturnsStandingsPerGroup()
    {
        var phase = Phase.Create("Pool", PhaseFormat.RoundRobin, 1, 2);
        var groupA = phase.Groups[0];
        var groupB = phase.Groups[1];

        groupA.TeamIds = ["a", "b"];
        groupB.TeamIds = ["c", "d"];

        var games = new List<Game>
        {
            CreateCompletedGame("a", "b", 2, 0),
            CreateGroupGame("c", "d", 2, 1, groupB.Id),
        };

        var result = phase.CalculateAllGroupStandings(games);

        result.Should().HaveCount(2);
        result[0].GroupId.Should().Be(groupA.Id);
        result[0].Standings.Should().HaveCount(2);
        result[0].Standings[0].TeamId.Should().Be("a");

        result[1].GroupId.Should().Be(groupB.Id);
        result[1].Standings.Should().HaveCount(2);
        result[1].Standings[0].TeamId.Should().Be("c");
    }

    [Fact]
    public void Phase_CalculateGroupStandings_ThrowsForUnknownGroup()
    {
        var phase = Phase.Create("Pool", PhaseFormat.RoundRobin, 1, 1);

        var act = () => phase.CalculateGroupStandings("nonexistent", []);

        act.Should().Throw<NotFoundException>();
    }

    private static Game CreateGroupGame(
        string homeTeamId, string awayTeamId, int homeScore, int awayScore,
        string groupId, int round = 1)
    {
        var game = Game.Create(TournamentId, PhaseId, groupId, round,
            homeTeamId: homeTeamId, awayTeamId: awayTeamId);
        game.RecordSimpleResult(homeScore, awayScore);
        return game;
    }
}
