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

        result.Should().HaveCount(3);
        result[0].Should().Be(new FinalStanding(1, "a", "Eagles", null));
        result[1].Should().Be(new FinalStanding(2, "b", "Hawks", null));
        result[2].Should().Be(new FinalStanding(3, "c", "Wolves", null));
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

        result.Should().HaveCount(4);
        result[0].Position.Should().Be(1);
        result[1].Position.Should().Be(2);
        result[2].Position.Should().Be(3);
        result[3].Position.Should().Be(4);
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

        result.Should().HaveCount(4);
        result[0].Should().Be(new FinalStanding(1, "b", "Hawks", null));
        result[1].Should().Be(new FinalStanding(2, "a", "Eagles", null));
        result[2].Should().Be(new FinalStanding(3, "c", "Wolves", null));
        result[3].Should().Be(new FinalStanding(4, "d", "Bears", null));
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

        result.Should().HaveCount(4);
        result[0].Should().Be(new FinalStanding(1, "a", "Eagles", null));
        result[1].Should().Be(new FinalStanding(2, "b", "Hawks", null));
        result[2].Should().Be(new FinalStanding(3, "c", "Wolves", null));
        result[3].Should().Be(new FinalStanding(4, "d", "Bears", null));
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

        result.Should().HaveCount(4);

        var goldStandings = result.Where(s => s.LevelName == "Level 1").ToList();
        goldStandings.Should().HaveCount(2);
        goldStandings[0].Should().Be(new FinalStanding(1, "a", "Eagles", "Level 1"));
        goldStandings[1].Should().Be(new FinalStanding(2, "b", "Hawks", "Level 1"));

        var silverStandings = result.Where(s => s.LevelName == "Level 2").ToList();
        silverStandings.Should().HaveCount(2);
        silverStandings[0].Should().Be(new FinalStanding(1, "c", "Wolves", "Level 2"));
        silverStandings[1].Should().Be(new FinalStanding(2, "d", "Bears", "Level 2"));
    }

    [Fact]
    public void WithLevels_TwoPhases_SameLevelCount_PlayoffLastPhase()
    {
        // Phase 1: RR with 2 levels, 1 group each, top 1 per level advances
        var phase1 = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 1,
            groupWinners: 1, numberOfLevels: 2);
        phase1.Status = PhaseStatus.Completed;
        var g1L1 = phase1.Groups.First(g => g.LevelId == phase1.Levels[0].Id);
        var g1L2 = phase1.Groups.First(g => g.LevelId == phase1.Levels[1].Id);
        g1L1.TeamIds.AddRange(["a", "b"]);
        g1L2.TeamIds.AddRange(["c", "d"]);

        // Phase 2: Elimination with 2 levels, 1 group each (same level count)
        var phase2 = Phase.Create("Playoff", PhaseFormat.PlayoffElimination, 2, 1,
            numberOfLevels: 2);
        phase2.Status = PhaseStatus.Completed;
        var g2L1 = phase2.Groups.First(g => g.LevelId == phase2.Levels[0].Id);
        var g2L2 = phase2.Groups.First(g => g.LevelId == phase2.Levels[1].Id);
        // After placeholder resolution, real teams assigned
        g2L1.TeamIds.Add("a");
        g2L2.TeamIds.Add("c");

        var games = new List<Game>
        {
            CompletedGame(phase1.Id, g1L1.Id, "a", "b", 2, 0),
            CompletedGame(phase1.Id, g1L2.Id, "c", "d", 2, 1),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears")
        };

        var result = FinalStandingsCalculator.Calculate([phase1, phase2], games, teams);

        result.Should().HaveCount(4);

        // Level 1: a (from playoff) at 1, b (eliminated in phase 1) at 2
        var level1 = result.Where(s => s.LevelName == "Level 1").ToList();
        level1.Should().HaveCount(2);
        level1[0].Should().Be(new FinalStanding(1, "a", "Eagles", "Level 1"));
        level1[1].Should().Be(new FinalStanding(2, "b", "Hawks", "Level 1"));

        // Level 2: c at 1, d at 2
        var level2 = result.Where(s => s.LevelName == "Level 2").ToList();
        level2.Should().HaveCount(2);
        level2[0].Should().Be(new FinalStanding(1, "c", "Wolves", "Level 2"));
        level2[1].Should().Be(new FinalStanding(2, "d", "Bears", "Level 2"));
    }

    [Fact]
    public void WithLevels_ThreePhases_NoLevelsThenLevelsThenMoreLevels()
    {
        // Phase 1: RR no levels, 4 teams, ALL advance (no non-advancing teams from pre-level phase)
        var phase1 = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 1,
            groupWinners: 4);
        phase1.Status = PhaseStatus.Completed;
        var g1 = phase1.Groups[0];
        g1.TeamIds.AddRange(["a", "b", "c", "d"]);

        // Phase 2: RR with 2 levels, 1 group each, top 1 per level advances
        var phase2 = Phase.Create("Intermediate", PhaseFormat.RoundRobin, 2, 1,
            groupWinners: 1, numberOfLevels: 2);
        phase2.Status = PhaseStatus.Completed;
        var g2L1 = phase2.Groups.First(g => g.LevelId == phase2.Levels[0].Id);
        var g2L2 = phase2.Groups.First(g => g.LevelId == phase2.Levels[1].Id);
        g2L1.TeamIds.AddRange(["a", "b"]); // Level 1
        g2L2.TeamIds.AddRange(["c", "d"]); // Level 2

        // Phase 3: Elimination with 4 levels (2→4 split), 1 group each
        var phase3 = Phase.Create("Playoff", PhaseFormat.PlayoffElimination, 3, 1,
            numberOfLevels: 4);
        phase3.Status = PhaseStatus.Completed;
        var g3L1 = phase3.Groups.First(g => g.LevelId == phase3.Levels[0].Id);
        var g3L2 = phase3.Groups.First(g => g.LevelId == phase3.Levels[1].Id);
        var g3L3 = phase3.Groups.First(g => g.LevelId == phase3.Levels[2].Id);
        var g3L4 = phase3.Groups.First(g => g.LevelId == phase3.Levels[3].Id);
        // Level 1-2 from Phase 2 Level 1, Level 3-4 from Phase 2 Level 2
        g3L1.TeamIds.Add("a");
        g3L2.TeamIds.Clear();
        g3L3.TeamIds.Add("c");
        g3L4.TeamIds.Clear();

        var games = new List<Game>
        {
            // Phase 1 RR: a > b > c > d
            CompletedGame(phase1.Id, g1.Id, "a", "b", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "a", "c", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "a", "d", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "b", "c", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "b", "d", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "c", "d", 2, 1),
            // Phase 2 RR: a > b in Level 1; c > d in Level 2
            CompletedGame(phase2.Id, g2L1.Id, "a", "b", 2, 0),
            CompletedGame(phase2.Id, g2L2.Id, "c", "d", 2, 0),
            // Phase 3: single teams per level, no games needed
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears")
        };

        var result = FinalStandingsCalculator.Calculate([phase1, phase2, phase3], games, teams);

        result.Should().HaveCount(4);

        // Level 1: a (phase 3 level 1) = 1st, b (eliminated phase 2 level 1) = 2nd
        var level1 = result.Where(s => s.LevelName == "Level 1").ToList();
        level1.Should().HaveCount(2);
        level1[0].Should().Be(new FinalStanding(1, "a", "Eagles", "Level 1"));
        level1[1].Should().Be(new FinalStanding(2, "b", "Hawks", "Level 1"));

        // Level 2: c (phase 3 level 3) = 1st, d (eliminated phase 2 level 2) = 2nd
        var level2 = result.Where(s => s.LevelName == "Level 2").ToList();
        level2.Should().HaveCount(2);
        level2[0].Should().Be(new FinalStanding(1, "c", "Wolves", "Level 2"));
        level2[1].Should().Be(new FinalStanding(2, "d", "Bears", "Level 2"));
    }

    [Fact]
    public void WithLevels_TwoPhases_LevelsSplitToMoreLevels()
    {
        // Phase 1: RR with 2 levels, 2 groups per level (4 groups total), top 2 per level
        var phase1 = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 1,
            groupWinners: 1, numberOfLevels: 2);
        phase1.Status = PhaseStatus.Completed;
        var g1L1 = phase1.Groups.First(g => g.LevelId == phase1.Levels[0].Id);
        var g1L2 = phase1.Groups.First(g => g.LevelId == phase1.Levels[1].Id);
        g1L1.TeamIds.AddRange(["a", "b"]);
        g1L2.TeamIds.AddRange(["c", "d"]);

        // Phase 2: Elimination with 4 levels (2→4 split)
        var phase2 = Phase.Create("Playoff", PhaseFormat.PlayoffElimination, 2, 1,
            numberOfLevels: 4);
        phase2.Status = PhaseStatus.Completed;
        var g2L1 = phase2.Groups.First(g => g.LevelId == phase2.Levels[0].Id);
        var g2L2 = phase2.Groups.First(g => g.LevelId == phase2.Levels[1].Id);
        var g2L3 = phase2.Groups.First(g => g.LevelId == phase2.Levels[2].Id);
        var g2L4 = phase2.Groups.First(g => g.LevelId == phase2.Levels[3].Id);
        // Phase1 Level1 → Phase2 Levels 1-2; Phase1 Level2 → Phase2 Levels 3-4
        g2L1.TeamIds.Add("a");
        g2L2.TeamIds.Clear();
        g2L3.TeamIds.Add("c");
        g2L4.TeamIds.Clear();

        var games = new List<Game>
        {
            CompletedGame(phase1.Id, g1L1.Id, "a", "b", 2, 0),
            CompletedGame(phase1.Id, g1L2.Id, "c", "d", 2, 1),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears")
        };

        var result = FinalStandingsCalculator.Calculate([phase1, phase2], games, teams);

        result.Should().HaveCount(4);

        // Level 1: a (from phase2 level 1) at 1, b (eliminated in phase 1 level 1) at 2
        var level1 = result.Where(s => s.LevelName == "Level 1").ToList();
        level1.Should().HaveCount(2);
        level1[0].Should().Be(new FinalStanding(1, "a", "Eagles", "Level 1"));
        level1[1].Should().Be(new FinalStanding(2, "b", "Hawks", "Level 1"));

        // Level 2: c (from phase2 level 3) at 1, d (eliminated in phase 1 level 2) at 2
        var level2 = result.Where(s => s.LevelName == "Level 2").ToList();
        level2.Should().HaveCount(2);
        level2[0].Should().Be(new FinalStanding(1, "c", "Wolves", "Level 2"));
        level2[1].Should().Be(new FinalStanding(2, "d", "Bears", "Level 2"));
    }

    // --- Not All Phases Completed ---

    [Fact]
    public void ReturnsEmpty_WhenNotAllPhasesCompleted()
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

        result.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsEmpty_WhenFirstPhaseStillInProgress()
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

        result.Should().BeEmpty();
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

        result.Should().HaveCount(2);
        result.Should().NotContain(s => s.TeamId == "p1");
    }

    // --- Playoff Elimination ---

    [Fact]
    public void SinglePhase_PlayoffElimination_PositionsByEliminationRound()
    {
        // 4-team single-elimination bracket: SF1, SF2, Final
        var phase = Phase.Create("Playoff", PhaseFormat.PlayoffElimination, 1, 1);
        phase.Status = PhaseStatus.Completed;
        var groupId = phase.Groups[0].Id;
        phase.Groups[0].TeamIds.AddRange(["a", "b", "c", "d"]);

        var games = new List<Game>
        {
            // SF: a beats b, d beats c
            CompletedGame(phase.Id, groupId, "a", "b", 2, 0, round: 1),
            CompletedGame(phase.Id, groupId, "d", "c", 2, 1, round: 1),
            // Final: a beats d
            CompletedGame(phase.Id, groupId, "a", "d", 2, 0, round: 2),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears")
        };

        var result = FinalStandingsCalculator.Calculate([phase], games, teams);

        result.Should().HaveCount(4);
        result[0].Should().Be(new FinalStanding(1, "a", "Eagles", null));
        result[1].Should().Be(new FinalStanding(2, "d", "Bears", null));
        // b and c both lost in SF (round 1) → positions 3 and 4 (both had position 3 in bracket)
        result[2].Position.Should().Be(3);
        result[3].Position.Should().Be(4);
    }

    [Fact]
    public void SinglePhase_PlayoffWithPlacement_UniquePositions()
    {
        // 4-team bracket with placement: SF1, SF2, 3rd place, Final
        var phase = Phase.Create("Playoff", PhaseFormat.PlayoffWithPlacement, 1, 1);
        phase.Status = PhaseStatus.Completed;
        var groupId = phase.Groups[0].Id;
        phase.Groups[0].TeamIds.AddRange(["a", "b", "c", "d"]);

        var games = new List<Game>
        {
            // SF (round 1): a beats b, c beats d
            CompletedGame(phase.Id, groupId, "a", "b", 2, 0, round: 1),
            CompletedGame(phase.Id, groupId, "c", "d", 2, 1, round: 1),
            // 3rd place (round 2): b beats d
            CompletedGame(phase.Id, groupId, "b", "d", 2, 0, round: 2),
            // Final (round 3): a beats c
            CompletedGame(phase.Id, groupId, "a", "c", 2, 1, round: 3),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears")
        };

        var result = FinalStandingsCalculator.Calculate([phase], games, teams);

        result.Should().HaveCount(4);
        result[0].Should().Be(new FinalStanding(1, "a", "Eagles", null));
        result[1].Should().Be(new FinalStanding(2, "c", "Wolves", null));
        result[2].Should().Be(new FinalStanding(3, "b", "Hawks", null));
        result[3].Should().Be(new FinalStanding(4, "d", "Bears", null));
    }

    [Fact]
    public void TwoPhases_RoundRobinThenPlayoffElimination()
    {
        // Phase 1: RR with 4 teams, top 2 advance
        var phase1 = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 1, groupWinners: 2);
        phase1.Status = PhaseStatus.Completed;
        var g1 = phase1.Groups[0];
        g1.TeamIds.AddRange(["a", "b", "c", "d"]);

        // Phase 2: Elimination with 2 teams (just a final)
        var phase2 = Phase.Create("Playoff", PhaseFormat.PlayoffElimination, 2, 1);
        phase2.Status = PhaseStatus.Completed;
        var g2 = phase2.Groups[0];
        g2.TeamIds.AddRange(["a", "b"]);

        var games = new List<Game>
        {
            // Phase 1 RR: a > b > c > d
            CompletedGame(phase1.Id, g1.Id, "a", "b", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "a", "c", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "a", "d", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "b", "c", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "b", "d", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "c", "d", 2, 1),
            // Phase 2 Final: b beats a
            CompletedGame(phase2.Id, g2.Id, "b", "a", 2, 0, round: 1),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears")
        };

        var result = FinalStandingsCalculator.Calculate([phase1, phase2], games, teams);

        result.Should().HaveCount(4);
        // Phase 2 (playoff): b=1st, a=2nd
        result[0].Should().Be(new FinalStanding(1, "b", "Hawks", null));
        result[1].Should().Be(new FinalStanding(2, "a", "Eagles", null));
        // Phase 1 non-advancing: c=3rd, d=4th
        result[2].Should().Be(new FinalStanding(3, "c", "Wolves", null));
        result[3].Should().Be(new FinalStanding(4, "d", "Bears", null));
    }

    [Fact]
    public void TwoPhases_RoundRobinThenPlayoffWithPlacement()
    {
        // Phase 1: RR with 6 teams, top 4 advance
        var phase1 = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 1, groupWinners: 4);
        phase1.Status = PhaseStatus.Completed;
        var g1 = phase1.Groups[0];
        g1.TeamIds.AddRange(["a", "b", "c", "d", "e", "f"]);

        // Phase 2: PlayoffWithPlacement with 4 teams
        var phase2 = Phase.Create("Playoff", PhaseFormat.PlayoffWithPlacement, 2, 1);
        phase2.Status = PhaseStatus.Completed;
        var g2 = phase2.Groups[0];
        g2.TeamIds.AddRange(["a", "b", "c", "d"]);

        var games = new List<Game>
        {
            // Phase 1 RR: a > b > c > d > e > f (each beats all below)
            CompletedGame(phase1.Id, g1.Id, "a", "b", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "a", "c", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "a", "d", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "a", "e", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "a", "f", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "b", "c", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "b", "d", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "b", "e", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "b", "f", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "c", "d", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "c", "e", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "c", "f", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "d", "e", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "d", "f", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "e", "f", 2, 1),
            // Phase 2 PlayoffWithPlacement: SF, 3rd place, Final
            CompletedGame(phase2.Id, g2.Id, "a", "d", 2, 0, round: 1), // SF1
            CompletedGame(phase2.Id, g2.Id, "b", "c", 2, 1, round: 1), // SF2
            CompletedGame(phase2.Id, g2.Id, "d", "c", 2, 0, round: 2), // 3rd place
            CompletedGame(phase2.Id, g2.Id, "a", "b", 2, 1, round: 3), // Final
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears"),
            RealTeam("e", "Lions"), RealTeam("f", "Tigers")
        };

        var result = FinalStandingsCalculator.Calculate([phase1, phase2], games, teams);

        result.Should().HaveCount(6);
        // Phase 2 (playoff with placement): a=1st, b=2nd, d=3rd, c=4th
        result[0].Should().Be(new FinalStanding(1, "a", "Eagles", null));
        result[1].Should().Be(new FinalStanding(2, "b", "Hawks", null));
        result[2].Should().Be(new FinalStanding(3, "d", "Bears", null));
        result[3].Should().Be(new FinalStanding(4, "c", "Wolves", null));
        // Phase 1 non-advancing: e=5th, f=6th
        result[4].Should().Be(new FinalStanding(5, "e", "Lions", null));
        result[5].Should().Be(new FinalStanding(6, "f", "Tigers", null));
    }

    // --- Zero Progression (Final Phase) ---

    [Fact]
    public void TwoPhases_ZeroProgression_AllTeamsGetPositionsFromFinalPhase()
    {
        // Phase 1: 4 teams, 2 advance
        var phase1 = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 1, groupWinners: 2);
        phase1.Status = PhaseStatus.Completed;
        var g1 = phase1.Groups[0];
        g1.TeamIds.AddRange(["a", "b", "c", "d"]);

        // Phase 2: 2 teams, zero progression (final phase)
        var phase2 = Phase.Create("Final", PhaseFormat.RoundRobin, 2, 1, groupWinners: 0, totalTeamsProceeding: 0);
        phase2.Status = PhaseStatus.Completed;
        var g2 = phase2.Groups[0];
        g2.TeamIds.AddRange(["a", "b"]);

        var games = new List<Game>
        {
            CompletedGame(phase1.Id, g1.Id, "a", "b", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "a", "c", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "a", "d", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "b", "c", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "b", "d", 2, 0),
            CompletedGame(phase1.Id, g1.Id, "c", "d", 2, 1),
            CompletedGame(phase2.Id, g2.Id, "b", "a", 2, 0),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "Eagles"), RealTeam("b", "Hawks"),
            RealTeam("c", "Wolves"), RealTeam("d", "Bears")
        };

        var result = FinalStandingsCalculator.Calculate([phase1, phase2], games, teams);

        result.Should().HaveCount(4);
        // Phase 2 positions 1-2 (all teams since zero progression = none advance)
        result[0].Should().Be(new FinalStanding(1, "b", "Hawks", null));
        result[1].Should().Be(new FinalStanding(2, "a", "Eagles", null));
        // Phase 1 non-advancing: c=3rd, d=4th
        result[2].Should().Be(new FinalStanding(3, "c", "Wolves", null));
        result[3].Should().Be(new FinalStanding(4, "d", "Bears", null));
    }

    [Fact]
    public void ThreePhases_SubLevelOrdering_HigherSubLevelTeamsRankFirst()
    {
        // Phase 1: RR no levels, 8 teams, all advance
        var phase1 = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 1,
            groupWinners: 8);
        phase1.Status = PhaseStatus.Completed;
        var g1 = phase1.Groups[0];
        g1.TeamIds.AddRange(["a", "b", "c", "d", "e", "f", "g", "h"]);

        // Phase 2: RR with 2 levels, 1 group each, all advance (4 per level)
        var phase2 = Phase.Create("Intermediate", PhaseFormat.RoundRobin, 2, 1,
            groupWinners: 2, numberOfLevels: 2);
        phase2.Status = PhaseStatus.Completed;
        var g2L1 = phase2.Groups.First(g => g.LevelId == phase2.Levels[0].Id);
        var g2L2 = phase2.Groups.First(g => g.LevelId == phase2.Levels[1].Id);
        g2L1.TeamIds.AddRange(["a", "b", "c", "d"]); // Level 1
        g2L2.TeamIds.AddRange(["e", "f", "g", "h"]); // Level 2

        // Phase 3: RR with 4 levels (2→4 split), 1 group each, zero progression (final phase)
        var phase3 = Phase.Create("Final", PhaseFormat.RoundRobin, 3, 1,
            groupWinners: 0, totalTeamsProceeding: 0, numberOfLevels: 4);
        phase3.Status = PhaseStatus.Completed;
        var g3L1 = phase3.Groups.First(g => g.LevelId == phase3.Levels[0].Id);
        var g3L2 = phase3.Groups.First(g => g.LevelId == phase3.Levels[1].Id);
        var g3L3 = phase3.Groups.First(g => g.LevelId == phase3.Levels[2].Id);
        var g3L4 = phase3.Groups.First(g => g.LevelId == phase3.Levels[3].Id);
        // Phase2 Level1 → Phase3 Levels 1-2; Phase2 Level2 → Phase3 Levels 3-4
        g3L1.TeamIds.AddRange(["a", "b"]);
        g3L2.TeamIds.AddRange(["c", "d"]);
        g3L3.TeamIds.AddRange(["e", "f"]);
        g3L4.TeamIds.AddRange(["g", "h"]);

        var games = new List<Game>
        {
            // Phase 1 RR: simplified — only adjacent pairs play since all 8 teams advance regardless
            CompletedGame(phase1.Id, g1.Id, "a", "b", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "c", "d", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "e", "f", 2, 1),
            CompletedGame(phase1.Id, g1.Id, "g", "h", 2, 1),
            // Phase 2 RR: a > b, c > d in Level 1; e > f, g > h in Level 2
            CompletedGame(phase2.Id, g2L1.Id, "a", "b", 2, 0),
            CompletedGame(phase2.Id, g2L1.Id, "c", "d", 2, 0),
            CompletedGame(phase2.Id, g2L1.Id, "a", "c", 2, 1),
            CompletedGame(phase2.Id, g2L1.Id, "b", "d", 2, 1),
            CompletedGame(phase2.Id, g2L1.Id, "a", "d", 2, 0),
            CompletedGame(phase2.Id, g2L1.Id, "b", "c", 2, 1),
            CompletedGame(phase2.Id, g2L2.Id, "e", "f", 2, 0),
            CompletedGame(phase2.Id, g2L2.Id, "g", "h", 2, 0),
            CompletedGame(phase2.Id, g2L2.Id, "e", "g", 2, 1),
            CompletedGame(phase2.Id, g2L2.Id, "f", "h", 2, 1),
            CompletedGame(phase2.Id, g2L2.Id, "e", "h", 2, 0),
            CompletedGame(phase2.Id, g2L2.Id, "f", "g", 2, 1),
            // Phase 3: a beats b in Level 1; c beats d in Level 2; e beats f in Level 3; g beats h in Level 4
            CompletedGame(phase3.Id, g3L1.Id, "a", "b", 2, 0),
            CompletedGame(phase3.Id, g3L2.Id, "c", "d", 2, 0),
            CompletedGame(phase3.Id, g3L3.Id, "e", "f", 2, 0),
            CompletedGame(phase3.Id, g3L4.Id, "g", "h", 2, 0),
        };

        var teams = new List<Team>
        {
            RealTeam("a", "T-a"), RealTeam("b", "T-b"),
            RealTeam("c", "T-c"), RealTeam("d", "T-d"),
            RealTeam("e", "T-e"), RealTeam("f", "T-f"),
            RealTeam("g", "T-g"), RealTeam("h", "T-h"),
        };

        var result = FinalStandingsCalculator.Calculate([phase1, phase2, phase3], games, teams);

        result.Should().HaveCount(8);

        // Root Level 1 pool: Phase3 Levels 1-2
        var level1 = result.Where(s => s.LevelName == "Level 1").ToList();
        level1.Should().HaveCount(4);
        level1[0].Should().Be(new FinalStanding(1, "a", "T-a", "Level 1"));
        level1[1].Should().Be(new FinalStanding(2, "b", "T-b", "Level 1"));
        level1[2].Should().Be(new FinalStanding(3, "c", "T-c", "Level 1"));
        level1[3].Should().Be(new FinalStanding(4, "d", "T-d", "Level 1"));

        // Root Level 2 pool: Phase3 Levels 3-4
        var level2 = result.Where(s => s.LevelName == "Level 2").ToList();
        level2.Should().HaveCount(4);
        level2[0].Should().Be(new FinalStanding(1, "e", "T-e", "Level 2"));
        level2[1].Should().Be(new FinalStanding(2, "f", "T-f", "Level 2"));
        level2[2].Should().Be(new FinalStanding(3, "g", "T-g", "Level 2"));
        level2[3].Should().Be(new FinalStanding(4, "h", "T-h", "Level 2"));
    }

    // --- Edge Cases ---

    [Fact]
    public void NoGames_ReturnsEmpty()
    {
        var phase = Phase.Create("P1", PhaseFormat.RoundRobin, 1, 1);
        phase.Status = PhaseStatus.Completed;
        phase.Groups[0].TeamIds.AddRange(["a", "b"]);

        var teams = new List<Team> { RealTeam("a", "Eagles"), RealTeam("b", "Hawks") };

        var result = FinalStandingsCalculator.Calculate([phase], [], teams);

        result.Should().BeEmpty();
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

        result.Should().HaveCount(2);
        result[0].Position.Should().Be(1);
        result[1].Position.Should().Be(2);
    }

    [Fact]
    public void EmptyPhasesReturnsEmpty()
    {
        var result = FinalStandingsCalculator.Calculate([], [], []);
        result.Should().BeEmpty();
    }
}
