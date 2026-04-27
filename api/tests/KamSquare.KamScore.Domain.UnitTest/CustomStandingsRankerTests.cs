using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Services.Formats;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.UnitTest;

public class CustomStandingsRankerTests
{
    private static Group GroupWith(IEnumerable<string> teamIds, IEnumerable<string>? order = null)
    {
        var group = Group.Create("A");
        foreach (var id in teamIds) group.AddTeam(id);
        group.ManualStandingOrder = order?.ToList() ?? [];
        return group;
    }

    // --- Calculate -------------------------------------------------------

    [Fact]
    public void Calculate_WithNoManualOrder_ReturnsEmptyList()
    {
        var group = GroupWith(["t1", "t2", "t3"]);

        var standings = CustomStandingsRanker.Calculate(group);

        standings.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_AssignsPositionsFromOrderIndex()
    {
        var group = GroupWith(["t1", "t2", "t3"], ["t3", "t1", "t2"]);

        var standings = CustomStandingsRanker.Calculate(group);

        standings.Should().HaveCount(3);
        standings.Single(s => s.TeamId == "t3").Position.Should().Be(1);
        standings.Single(s => s.TeamId == "t1").Position.Should().Be(2);
        standings.Single(s => s.TeamId == "t2").Position.Should().Be(3);
    }

    [Fact]
    public void Calculate_LeavesStatsBlank()
    {
        var group = GroupWith(["t1", "t2"], ["t2", "t1"]);

        var standings = CustomStandingsRanker.Calculate(group);

        standings.Should().AllSatisfy(s =>
        {
            s.GamesPlayed.Should().Be(0);
            s.Wins.Should().Be(0);
            s.Draws.Should().Be(0);
            s.Losses.Should().Be(0);
            s.Points.Should().BeNull();
            s.SetsWon.Should().BeNull();
            s.SetsLost.Should().BeNull();
            s.SetDifference.Should().BeNull();
            s.PointsWon.Should().BeNull();
            s.PointsLost.Should().BeNull();
            s.PointDifference.Should().BeNull();
        });
    }

    // --- RankCrossGroup -------------------------------------------------

    [Fact]
    public void RankCrossGroup_OrdersByPositionAscending()
    {
        var standings = new List<Standing>
        {
            new("t-g1-2", 2, 0, 0, 0, 0, null, null, null, null, null, null, null),
            new("t-g2-1", 1, 0, 0, 0, 0, null, null, null, null, null, null, null),
            new("t-g1-1", 1, 0, 0, 0, 0, null, null, null, null, null, null, null),
            new("t-g2-2", 2, 0, 0, 0, 0, null, null, null, null, null, null, null),
        };

        var ranked = CustomStandingsRanker.RankCrossGroup(standings);

        ranked.Should().HaveCount(4);
        ranked.Take(2).Select(s => s.Position).Should().AllBeEquivalentTo(1);
        ranked.Skip(2).Select(s => s.Position).Should().AllBeEquivalentTo(2);
    }

    [Fact]
    public void RankCrossGroup_IsStable_ForEqualPositions()
    {
        // Same position across groups — order follows input order.
        var standings = new List<Standing>
        {
            new("first-group-winner", 1, 0, 0, 0, 0, null, null, null, null, null, null, null),
            new("second-group-winner", 1, 0, 0, 0, 0, null, null, null, null, null, null, null),
        };

        var ranked = CustomStandingsRanker.RankCrossGroup(standings);

        ranked[0].TeamId.Should().Be("first-group-winner");
        ranked[1].TeamId.Should().Be("second-group-winner");
    }
}
