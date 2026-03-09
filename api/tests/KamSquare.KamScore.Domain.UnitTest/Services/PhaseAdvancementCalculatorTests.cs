using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.UnitTest.Services;

public class PhaseAdvancementCalculatorTests
{
    private static Standing MakeStanding(string teamId, int position, int points = 0,
        int setDiff = 0, int pointDiff = 0, int wins = 0, int losses = 0) =>
        new(teamId, position, wins + losses, wins, 0, losses,
            points, points > 0 ? wins : null, points > 0 ? losses : null,
            setDiff, null, null, pointDiff);

    // --- CalculateQualifyingTeamIds ---

    [Fact]
    public void CalculateQualifyingTeamIds_GroupWinnersOnly_TakesTopFromEachGroup()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2, groupWinners: 2);
        var groupStandings = new List<(string, List<Standing>)>
        {
            ("g1", [MakeStanding("t1", 1, 6), MakeStanding("t2", 2, 4), MakeStanding("t3", 3, 2), MakeStanding("t4", 4, 0)]),
            ("g2", [MakeStanding("t5", 1, 6), MakeStanding("t6", 2, 4), MakeStanding("t7", 3, 2), MakeStanding("t8", 4, 0)])
        };

        var result = PhaseAdvancementCalculator.CalculateQualifyingTeamIds(phase, groupStandings);

        result.Should().HaveCount(4);
        result.Should().Contain(["t1", "t2", "t5", "t6"]);
    }

    [Fact]
    public void CalculateQualifyingTeamIds_TotalTeamsProceedingOnly_TakesTopAcrossGroups()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2,
            totalTeamsProceeding: 4);
        var groupStandings = new List<(string, List<Standing>)>
        {
            ("g1", [MakeStanding("t1", 1, 6, 3), MakeStanding("t2", 2, 4, 1), MakeStanding("t3", 3, 2), MakeStanding("t4", 4, 0)]),
            ("g2", [MakeStanding("t5", 1, 6, 2), MakeStanding("t6", 2, 4, 2), MakeStanding("t7", 3, 2), MakeStanding("t8", 4, 0)])
        };

        var result = PhaseAdvancementCalculator.CalculateQualifyingTeamIds(phase, groupStandings);

        result.Should().HaveCount(4);
        result.Should().Contain(["t1", "t5", "t2", "t6"]);
    }

    [Fact]
    public void CalculateQualifyingTeamIds_BothSet_GroupWinnersPlusBestRemaining()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2,
            groupWinners: 1, totalTeamsProceeding: 4);
        var groupStandings = new List<(string, List<Standing>)>
        {
            ("g1", [MakeStanding("t1", 1, 6), MakeStanding("t2", 2, 4, 2), MakeStanding("t3", 3, 2), MakeStanding("t4", 4, 0)]),
            ("g2", [MakeStanding("t5", 1, 6), MakeStanding("t6", 2, 4, 1), MakeStanding("t7", 3, 2), MakeStanding("t8", 4, 0)])
        };

        var result = PhaseAdvancementCalculator.CalculateQualifyingTeamIds(phase, groupStandings);

        result.Should().HaveCount(4);
        // Group winners: t1, t5. Best remaining: t2 (setDiff=2), t6 (setDiff=1)
        result.Should().Contain(["t1", "t5", "t2", "t6"]);
    }

    [Fact]
    public void CalculateQualifyingTeamIds_BothNull_ReturnsEmpty()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2);
        var groupStandings = new List<(string, List<Standing>)>
        {
            ("g1", [MakeStanding("t1", 1, 6)]),
            ("g2", [MakeStanding("t2", 1, 6)])
        };

        var result = PhaseAdvancementCalculator.CalculateQualifyingTeamIds(phase, groupStandings);

        result.Should().BeEmpty();
    }

    // --- CalculateSeeding ---

    [Fact]
    public void CalculateSeeding_RanksAllTeamsTogether()
    {
        var groupStandings = new List<(string, List<Standing>)>
        {
            ("g1", [MakeStanding("t1", 1, 6, 5, 10, 3), MakeStanding("t2", 2, 4, 2, 5, 2)]),
            ("g2", [MakeStanding("t5", 1, 6, 3, 8, 3), MakeStanding("t6", 2, 4, 3, 7, 2)])
        };

        var result = PhaseAdvancementCalculator.CalculateSeeding(
            ["t1", "t5", "t2", "t6"], groupStandings);

        result.Should().HaveCount(4);
        // t1: 6pts, 5sd, 10pd — best
        // t5: 6pts, 3sd, 8pd — second
        // t6: 4pts, 3sd, 7pd — third
        // t2: 4pts, 2sd, 5pd — fourth
        result[0].Should().Be("t1");
        result[1].Should().Be("t5");
        result[2].Should().Be("t6");
        result[3].Should().Be("t2");
    }

    [Fact]
    public void CalculateSeeding_OnlyIncludesQualifyingTeams()
    {
        var groupStandings = new List<(string, List<Standing>)>
        {
            ("g1", [MakeStanding("t1", 1, 6), MakeStanding("t2", 2, 4), MakeStanding("t3", 3, 0)])
        };

        var result = PhaseAdvancementCalculator.CalculateSeeding(["t1", "t2"], groupStandings);

        result.Should().HaveCount(2);
        result.Should().NotContain("t3");
    }

    // --- GetExpectedTeamCount ---

    [Fact]
    public void GetExpectedTeamCount_TotalTeamsProceeding_ReturnsThat()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2,
            groupWinners: 2, totalTeamsProceeding: 6);

        PhaseAdvancementCalculator.GetExpectedTeamCount(phase).Should().Be(6);
    }

    [Fact]
    public void GetExpectedTeamCount_OnlyGroupWinners_ReturnsGroupWinnersTimesGroups()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 3, groupWinners: 2);

        PhaseAdvancementCalculator.GetExpectedTeamCount(phase).Should().Be(6);
    }

    [Fact]
    public void GetExpectedTeamCount_NeitherSet_ReturnsNull()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2);

        PhaseAdvancementCalculator.GetExpectedTeamCount(phase).Should().BeNull();
    }

    // --- Level-Aware Tests ---

    [Fact]
    public void CalculateQualifyingTeamIds_WithLevels_QualifiesPerLevel()
    {
        // 2 levels, 2 groups each, TotalTeamsProceeding=2 per level → 4 total
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 2, 2,
            totalTeamsProceeding: 2, numberOfLevels: 2);

        var level1GroupIds = phase.Groups.Where(g => g.LevelId == phase.Levels[0].Id).Select(g => g.Id).ToList();
        var level2GroupIds = phase.Groups.Where(g => g.LevelId == phase.Levels[1].Id).Select(g => g.Id).ToList();

        var groupStandings = new List<(string, List<Standing>)>
        {
            (level1GroupIds[0], [MakeStanding("L1-t1", 1, 6, 3), MakeStanding("L1-t2", 2, 4, 1)]),
            (level1GroupIds[1], [MakeStanding("L1-t3", 1, 6, 2), MakeStanding("L1-t4", 2, 4, 0)]),
            (level2GroupIds[0], [MakeStanding("L2-t1", 1, 6, 3), MakeStanding("L2-t2", 2, 4, 1)]),
            (level2GroupIds[1], [MakeStanding("L2-t3", 1, 6, 2), MakeStanding("L2-t4", 2, 4, 0)])
        };

        var result = PhaseAdvancementCalculator.CalculateQualifyingTeamIds(phase, groupStandings);

        result.Should().HaveCount(4); // 2 per level × 2 levels
        // Level 1 qualifiers: L1-t1 (6pts, 3sd), L1-t3 (6pts, 2sd) — top 2 across L1 groups
        result.Should().Contain(["L1-t1", "L1-t3"]);
        // Level 2 qualifiers: L2-t1 (6pts, 3sd), L2-t3 (6pts, 2sd) — top 2 across L2 groups
        result.Should().Contain(["L2-t1", "L2-t3"]);
    }

    [Fact]
    public void CalculateQualifyingTeamIds_WithLevels_GroupWinnersPerLevel()
    {
        // 2 levels, 2 groups each, GroupWinners=1 per level → 1 per group × 4 groups = 4 total
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 2, 2,
            groupWinners: 1, numberOfLevels: 2);

        var level1GroupIds = phase.Groups.Where(g => g.LevelId == phase.Levels[0].Id).Select(g => g.Id).ToList();
        var level2GroupIds = phase.Groups.Where(g => g.LevelId == phase.Levels[1].Id).Select(g => g.Id).ToList();

        var groupStandings = new List<(string, List<Standing>)>
        {
            (level1GroupIds[0], [MakeStanding("L1-A1", 1, 6), MakeStanding("L1-A2", 2, 2)]),
            (level1GroupIds[1], [MakeStanding("L1-B1", 1, 6), MakeStanding("L1-B2", 2, 2)]),
            (level2GroupIds[0], [MakeStanding("L2-A1", 1, 6), MakeStanding("L2-A2", 2, 2)]),
            (level2GroupIds[1], [MakeStanding("L2-B1", 1, 6), MakeStanding("L2-B2", 2, 2)])
        };

        var result = PhaseAdvancementCalculator.CalculateQualifyingTeamIds(phase, groupStandings);

        result.Should().HaveCount(4);
        result.Should().Contain(["L1-A1", "L1-B1", "L2-A1", "L2-B1"]);
    }

    [Fact]
    public void CalculateSeeding_WithLevels_RanksLevel1AboveLevel2()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 2, 2,
            groupWinners: 1, numberOfLevels: 2);

        var level1GroupIds = phase.Groups.Where(g => g.LevelId == phase.Levels[0].Id).Select(g => g.Id).ToList();
        var level2GroupIds = phase.Groups.Where(g => g.LevelId == phase.Levels[1].Id).Select(g => g.Id).ToList();

        var groupStandings = new List<(string, List<Standing>)>
        {
            (level1GroupIds[0], [MakeStanding("L1-A1", 1, 6, 3, 10, 3)]),
            (level1GroupIds[1], [MakeStanding("L1-B1", 1, 6, 1, 5, 3)]),
            (level2GroupIds[0], [MakeStanding("L2-A1", 1, 6, 5, 15, 3)]),
            (level2GroupIds[1], [MakeStanding("L2-B1", 1, 6, 4, 12, 3)])
        };

        var qualifyingIds = new List<string> { "L1-A1", "L1-B1", "L2-A1", "L2-B1" };

        var result = PhaseAdvancementCalculator.CalculateSeeding(qualifyingIds, groupStandings, phase);

        result.Should().HaveCount(4);
        // Level 1 ranked first: L1-A1 (sd=3) before L1-B1 (sd=1)
        result[0].Should().Be("L1-A1");
        result[1].Should().Be("L1-B1");
        // Level 2 ranked after: L2-A1 (sd=5) before L2-B1 (sd=4)
        // Even though L2 teams have better stats, they rank below L1
        result[2].Should().Be("L2-A1");
        result[3].Should().Be("L2-B1");
    }

    [Fact]
    public void CalculateSeeding_WithoutLevels_Unchanged()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2, groupWinners: 1);
        var groupStandings = new List<(string, List<Standing>)>
        {
            (phase.Groups[0].Id, [MakeStanding("t1", 1, 6, 5, 10, 3)]),
            (phase.Groups[1].Id, [MakeStanding("t2", 1, 6, 3, 8, 3)])
        };

        var result = PhaseAdvancementCalculator.CalculateSeeding(["t1", "t2"], groupStandings, phase);

        result.Should().Equal("t1", "t2");
    }

    [Fact]
    public void GetExpectedTeamCount_WithLevels_MultipliesByLevelCount()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 2, 2,
            totalTeamsProceeding: 4, numberOfLevels: 2);

        PhaseAdvancementCalculator.GetExpectedTeamCount(phase).Should().Be(8); // 4 per level × 2 levels
    }

    [Fact]
    public void GetExpectedTeamCount_WithLevels_GroupWinnersUsesAllGroups()
    {
        // 2 levels × 2 groups = 4 total groups, GroupWinners=1 → 4 teams
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 2, 2,
            groupWinners: 1, numberOfLevels: 2);

        PhaseAdvancementCalculator.GetExpectedTeamCount(phase).Should().Be(4); // 1 × 4 groups
    }
}
