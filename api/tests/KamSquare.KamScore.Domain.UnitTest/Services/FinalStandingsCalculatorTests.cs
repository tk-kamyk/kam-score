using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.UnitTest.Services;

public class FinalStandingsCalculatorTests
{
    private static Game CompletedGame(string phaseId, string groupId, string home, string away,
        int homeScore, int awayScore, int round = 1)
    {
        var game = Game.Create("t1", phaseId, groupId, round, homeTeamId: home, awayTeamId: away);
        game.RecordSimpleResult(homeScore, awayScore);
        return game;
    }

    private static Team RealTeam(string id, string name) =>
        new() { Id = id, Name = name, TournamentId = "t1" };

    private static Team PlaceholderTeam(string id, string name, string sourcePhaseId, int seed) =>
        new()
        {
            Id = id, Name = name, TournamentId = "t1",
            IsPlaceholder = true, SourcePhaseId = sourcePhaseId, Seed = seed
        };

    // --- Single Phase ---

    [Fact]
    public void SinglePhase_RoundRobin_ReturnsGroupStandingsDirectly()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 1);
        phase.Status = PhaseStatus.Completed;
        var groupId = phase.Groups[0].Id;
        phase.Groups[0].TeamIds.AddRange(["a", "b", "c"]);

        var games = new List<Game>
        {
            CompletedGame(phase.Id, groupId, "a", "b", 2, 0),
            CompletedGame(phase.Id, groupId, "a", "c", 2, 1),
            CompletedGame(phase.Id, groupId, "b", "c", 2, 0),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"), RealTeam("c", "Wolves")
        };

        var result = FinalStandingsCalculator.Calculate([phase], games, teams);

        result.Provisional.Should().BeFalse();
        result.Standings.Should().HaveCount(3);
        result.Standings[0].Should().Be(new FinalStanding(1, "a", "Eagles", null));
        result.Standings[1].Should().Be(new FinalStanding(2, "b", "Hawks", null));
        result.Standings[2].Should().Be(new FinalStanding(3, "c", "Wolves", null));
    }

    [Fact]
    public void SinglePhase_MultipleGroups_MergesByCrossGroupRanking()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2);
        phase.Status = PhaseStatus.Completed;
        var g1 = phase.Groups[0];
        var g2 = phase.Groups[1];
        g1.TeamIds.AddRange(["a", "b"]);
        g2.TeamIds.AddRange(["c", "d"]);

        var games = new List<Game>
        {
            CompletedGame(phase.Id, g1.Id, "a", "b", 2, 1),
            CompletedGame(phase.Id, g2.Id, "c", "d", 2, 0),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears")
        };

        var result = FinalStandingsCalculator.Calculate([phase], games, teams);

        result.Standings.Should().HaveCount(4);
        // All group winners first (by cross-group ranking), then losers
        result.Standings[0].Position.Should().Be(1);
        result.Standings[1].Position.Should().Be(2);
        result.Standings[2].Position.Should().Be(3);
        result.Standings[3].Position.Should().Be(4);
    }

    // --- Multi-Phase ---

    [Fact]
    public void TwoPhases_EliminatedTeamsGetLowerPositions()
    {
        // Phase 1: 4 teams, 2 advance
        var phase1 = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 1,
            groupWinners: 2);
        phase1.Status = PhaseStatus.Completed;
        var g1 = phase1.Groups[0];
        g1.TeamIds.AddRange(["a", "b", "c", "d"]);

        // Phase 2: 2 teams
        var phase2 = Phase.Create("Final", PhaseFormat.RoundRobin, 2, 1);
        phase2.Status = PhaseStatus.Completed;
        var g2 = phase2.Groups[0];
        g2.TeamIds.AddRange(["a", "b"]);

        var games = new List<Game>
        {
            // Phase 1: a beats everyone, b beats c and d, c beats d
            CompletedGame(phase1.Id, g1.Id, "a", "b", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "a", "c", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "a", "d", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "b", "c", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "b", "d", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "c", "d", 2, 1),
            // Phase 2: b beats a
            CompletedGame(phase2.Id, g2.Id, "b", "a", 2, 0),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears")
        };

        var result = FinalStandingsCalculator.Calculate([phase1, phase2], games, teams);

        result.Provisional.Should().BeFalse();
        result.Standings.Should().HaveCount(4);
        result.Standings[0].Should().Be(new FinalStanding(1, "b", "Hawks", null));
        result.Standings[1].Should().Be(new FinalStanding(2, "a", "Eagles", null));
        result.Standings[2].Should().Be(new FinalStanding(3, "c", "Wolves", null));
        result.Standings[3].Should().Be(new FinalStanding(4, "d", "Bears", null));
    }

    [Fact]
    public void ThreePhases_ProgressiveElimination()
    {
        var phase1 = Phase.Create("P1", PhaseFormat.RoundRobin, 1, 1, groupWinners: 2);
        phase1.Status = PhaseStatus.Completed;
        phase1.Groups[0].TeamIds.AddRange(["a", "b", "c", "d"]);

        var phase2 = Phase.Create("P2", PhaseFormat.RoundRobin, 2, 1, groupWinners: 1);
        phase2.Status = PhaseStatus.Completed;
        phase2.Groups[0].TeamIds.AddRange(["a", "b"]);

        var phase3 = Phase.Create("P3", PhaseFormat.RoundRobin, 3, 1);
        phase3.Status = PhaseStatus.Completed;
        phase3.Groups[0].TeamIds.AddRange(["a"]);

        var games = new List<Game>
        {
            // P1: a > b > c > d
            CompletedGame(phase1.Id, phase1.Groups[0].Id, "a", "b", 2, 1),
            CompletedGame(phase1.Id, phase1.Groups[0].Id, "a", "c", 2, 0),
            CompletedGame(phase1.Id, phase1.Groups[0].Id, "a", "d", 2, 0),
            CompletedGame(phase1.Id, phase1.Groups[0].Id, "b", "c", 2, 1),
            CompletedGame(phase1.Id, phase1.Groups[0].Id, "b", "d", 2, 0),
            CompletedGame(phase1.Id, phase1.Groups[0].Id, "c", "d", 2, 1),
            // P2: a beats b
            CompletedGame(phase2.Id, phase2.Groups[0].Id, "a", "b", 2, 0),
            // P3: just a (single team, no games needed but let's keep standings working)
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears")
        };

        var result = FinalStandingsCalculator.Calculate([phase1, phase2, phase3], games, teams);

        result.Standings.Should().HaveCount(4);
        result.Standings[0].Should().Be(new FinalStanding(1, "a", "Eagles", null));
        result.Standings[1].Should().Be(new FinalStanding(2, "b", "Hawks", null));
        result.Standings[2].Should().Be(new FinalStanding(3, "c", "Wolves", null));
        result.Standings[3].Should().Be(new FinalStanding(4, "d", "Bears", null));
    }

    // --- Levels ---

    [Fact]
    public void WithLevels_ProducesPerLevelStandings()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 1,
            numberOfLevels: 2);
        phase.Status = PhaseStatus.Completed;
        // 2 levels, 1 group each = 2 groups total
        var goldGroup = phase.Groups.First(g => g.LevelId == phase.Levels[0].Id);
        var silverGroup = phase.Groups.First(g => g.LevelId == phase.Levels[1].Id);
        goldGroup.TeamIds.AddRange(["a", "b"]);
        silverGroup.TeamIds.AddRange(["c", "d"]);

        var games = new List<Game>
        {
            CompletedGame(phase.Id, goldGroup.Id, "a", "b", 2, 0),
            CompletedGame(phase.Id, silverGroup.Id, "c", "d", 2, 1),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears")
        };

        var result = FinalStandingsCalculator.Calculate([phase], games, teams);

        result.Standings.Should().HaveCount(4);

        var goldStandings = result.Standings.Where(s => s.LevelName == "Level 1").ToList();
        goldStandings.Should().HaveCount(2);
        goldStandings[0].Should().Be(new FinalStanding(1, "a", "Eagles", "Level 1"));
        goldStandings[1].Should().Be(new FinalStanding(2, "b", "Hawks", "Level 1"));

        var silverStandings = result.Standings.Where(s => s.LevelName == "Level 2").ToList();
        silverStandings.Should().HaveCount(2);
        silverStandings[0].Should().Be(new FinalStanding(1, "c", "Wolves", "Level 2"));
        silverStandings[1].Should().Be(new FinalStanding(2, "d", "Bears", "Level 2"));
    }

    // --- Provisional ---

    [Fact]
    public void ProvisionalWhenLastPhaseIsInProgress()
    {
        var phase1 = Phase.Create("P1", PhaseFormat.RoundRobin, 1, 1, groupWinners: 1);
        phase1.Status = PhaseStatus.Completed;
        phase1.Groups[0].TeamIds.AddRange(["a", "b"]);

        var phase2 = Phase.Create("P2", PhaseFormat.RoundRobin, 2, 1);
        phase2.Status = PhaseStatus.InProgress;
        phase2.Groups[0].TeamIds.AddRange(["a"]);

        var games = new List<Game>
        {
            CompletedGame(phase1.Id, phase1.Groups[0].Id, "a", "b", 2, 0),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
        };

        var result = FinalStandingsCalculator.Calculate([phase1, phase2], games, teams);

        result.Provisional.Should().BeTrue();
        result.Standings.Should().HaveCount(2);
        // b is eliminated from P1
        result.Standings.Last().Should().Be(new FinalStanding(2, "b", "Hawks", null));
    }

    [Fact]
    public void ProvisionalWhenOnlyFirstPhaseHasGames()
    {
        var phase1 = Phase.Create("P1", PhaseFormat.RoundRobin, 1, 1, groupWinners: 1);
        phase1.Status = PhaseStatus.InProgress;
        phase1.Groups[0].TeamIds.AddRange(["a", "b"]);

        var phase2 = Phase.Create("P2", PhaseFormat.RoundRobin, 2, 1);
        phase2.Status = PhaseStatus.New;
        phase2.Groups[0].TeamIds.AddRange(["a"]);

        var games = new List<Game>
        {
            CompletedGame(phase1.Id, phase1.Groups[0].Id, "a", "b", 2, 0),
        };

        var teams = new List<Team> { RealTeam("a", "Eagles"), RealTeam("b", "Hawks") };

        var result = FinalStandingsCalculator.Calculate([phase1, phase2], games, teams);

        result.Provisional.Should().BeTrue();
        result.Standings.Should().HaveCount(2);
    }

    [Fact]
    public void NotProvisionalWhenAllPhasesCompleted()
    {
        var phase = Phase.Create("P1", PhaseFormat.RoundRobin, 1, 1);
        phase.Status = PhaseStatus.Completed;
        phase.Groups[0].TeamIds.AddRange(["a", "b"]);

        var games = new List<Game>
        {
            CompletedGame(phase.Id, phase.Groups[0].Id, "a", "b", 2, 0),
        };

        var teams = new List<Team> { RealTeam("a", "Eagles"), RealTeam("b", "Hawks") };

        var result = FinalStandingsCalculator.Calculate([phase], games, teams);

        result.Provisional.Should().BeFalse();
    }

    // --- Placeholder Exclusion ---

    [Fact]
    public void PlaceholderTeamsAreExcluded()
    {
        var phase = Phase.Create("P1", PhaseFormat.RoundRobin, 1, 1);
        phase.Status = PhaseStatus.Completed;
        phase.Groups[0].TeamIds.AddRange(["a", "b"]);

        var games = new List<Game>
        {
            CompletedGame(phase.Id, phase.Groups[0].Id, "a", "b", 2, 0),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            PlaceholderTeam("p1", "P1 - Seed 1", phase.Id, 1),
        };

        var result = FinalStandingsCalculator.Calculate([phase], games, teams);

        result.Standings.Should().HaveCount(2);
        result.Standings.Should().NotContain(s => s.TeamId == "p1");
    }

    // --- Edge Cases ---

    [Fact]
    public void NoGames_ReturnsEmpty()
    {
        var phase = Phase.Create("P1", PhaseFormat.RoundRobin, 1, 1);
        phase.Groups[0].TeamIds.AddRange(["a", "b"]);

        var teams = new List<Team> { RealTeam("a", "Eagles"), RealTeam("b", "Hawks") };

        var result = FinalStandingsCalculator.Calculate([phase], [], teams);

        result.Standings.Should().BeEmpty();
    }

    [Fact]
    public void PhaseWithoutProgressionConfig_IsLastPhase()
    {
        var phase = Phase.Create("P1", PhaseFormat.RoundRobin, 1, 1);
        phase.Status = PhaseStatus.Completed;
        phase.Groups[0].TeamIds.AddRange(["a", "b"]);

        var games = new List<Game>
        {
            CompletedGame(phase.Id, phase.Groups[0].Id, "a", "b", 2, 0),
        };

        var teams = new List<Team> { RealTeam("a", "Eagles"), RealTeam("b", "Hawks") };

        var result = FinalStandingsCalculator.Calculate([phase], games, teams);

        result.Standings.Should().HaveCount(2);
        result.Standings[0].Position.Should().Be(1);
        result.Standings[1].Position.Should().Be(2);
    }

    [Fact]
    public void EmptyPhasesReturnsEmpty()
    {
        var result = FinalStandingsCalculator.Calculate([], [], []);
        result.Standings.Should().BeEmpty();
        result.Provisional.Should().BeFalse();
    }
}
