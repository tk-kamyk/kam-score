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
}
