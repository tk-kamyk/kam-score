using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.UnitTest;

public class GroupTests
{
    [Fact]
    public void ManualStandingOrder_DefaultsToEmpty()
    {
        var group = Group.Create("A");

        group.ManualStandingOrder.Should().BeEmpty();
    }

    [Fact]
    public void SetManualStandingOrder_StoresCompleteOrdering()
    {
        var group = GroupWithTeams("t1", "t2", "t3");

        group.SetManualStandingOrder(["t3", "t1", "t2"]);

        group.ManualStandingOrder.Should().BeEquivalentTo(["t3", "t1", "t2"], opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void SetManualStandingOrder_RejectsForeignTeamId()
    {
        var group = GroupWithTeams("t1", "t2", "t3");

        var act = () => group.SetManualStandingOrder(["t1", "t-other", "t3"]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetManualStandingOrder_RejectsDuplicateTeamId()
    {
        var group = GroupWithTeams("t1", "t2", "t3");

        var act = () => group.SetManualStandingOrder(["t1", "t1", "t3"]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetManualStandingOrder_RejectsPartialOrdering()
    {
        var group = GroupWithTeams("t1", "t2", "t3");

        var act = () => group.SetManualStandingOrder(["t1", "t2"]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetManualStandingOrder_RejectsTooManyTeams()
    {
        var group = GroupWithTeams("t1", "t2", "t3");

        var act = () => group.SetManualStandingOrder(["t1", "t2", "t3", "t1"]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ClearManualStandingOrder_EmptiesTheList()
    {
        var group = GroupWithTeams("t1", "t2");
        group.SetManualStandingOrder(["t2", "t1"]);

        group.ClearManualStandingOrder();

        group.ManualStandingOrder.Should().BeEmpty();
    }

    [Fact]
    public void ClearTeams_AlsoClearsManualStandingOrder()
    {
        var group = GroupWithTeams("t1", "t2");
        group.SetManualStandingOrder(["t1", "t2"]);

        group.ClearTeams();

        group.ManualStandingOrder.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTeam_ClearsManualStandingOrder()
    {
        var group = GroupWithTeams("t1", "t2", "t3");
        group.SetManualStandingOrder(["t1", "t2", "t3"]);

        group.RemoveTeam("t2");

        group.ManualStandingOrder.Should().BeEmpty();
    }

    private static Group GroupWithTeams(params string[] teamIds)
    {
        var group = Group.Create("A");
        foreach (var id in teamIds) group.AddTeam(id);
        return group;
    }
}
