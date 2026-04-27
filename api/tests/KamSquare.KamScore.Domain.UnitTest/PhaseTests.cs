using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Domain.UnitTest;

public class PhaseTests
{
    [Fact]
    public void Create_Custom_SupportsLevelsLikeOtherFormats()
    {
        var phase = Phase.Create("Custom Phase", PhaseFormat.Custom, order: 1,
            numberOfGroups: 2, numberOfLevels: 2);

        phase.Format.Should().Be(PhaseFormat.Custom);
        phase.Levels.Should().HaveCount(2);
        phase.Groups.Should().HaveCount(4); // 2 levels × 2 groups per level
    }

    [Fact]
    public void Complete_CustomPhase_TransitionsInProgressToCompleted()
    {
        var phase = Phase.Create("Custom Phase", PhaseFormat.Custom, order: 1, numberOfGroups: 1);
        phase.Activate();

        phase.Complete();

        phase.Status.Should().Be(PhaseStatus.Completed);
    }

    [Fact]
    public void Update_ChangingFormatAwayFromCustom_ClearsAllManualStandingOrders()
    {
        var phase = Phase.Create("Phase", PhaseFormat.Custom, order: 1, numberOfGroups: 2);
        foreach (var group in phase.Groups)
        {
            group.AddTeam("t1");
            group.AddTeam("t2");
            group.SetManualStandingOrder(["t2", "t1"]);
        }

        phase.Update("Phase", PhaseFormat.RoundRobin, null, null, null);

        phase.Groups.Should().AllSatisfy(g => g.ManualStandingOrder.Should().BeEmpty());
    }

    [Fact]
    public void Update_ChangingFormatToCustom_ClearsStaleManualStandingOrders()
    {
        var phase = Phase.Create("Phase", PhaseFormat.RoundRobin, order: 1, numberOfGroups: 1);
        // Simulate a stale ordering left over from a previous Custom phase config.
        phase.Groups[0].AddTeam("t1");
        phase.Groups[0].AddTeam("t2");
        phase.Groups[0].ManualStandingOrder = ["t2", "t1"];

        phase.Update("Phase", PhaseFormat.Custom, null, null, null);

        phase.Groups[0].ManualStandingOrder.Should().BeEmpty();
    }

    [Fact]
    public void Update_SameFormat_PreservesManualStandingOrder()
    {
        var phase = Phase.Create("Phase", PhaseFormat.Custom, order: 1, numberOfGroups: 1);
        phase.Groups[0].AddTeam("t1");
        phase.Groups[0].AddTeam("t2");
        phase.Groups[0].SetManualStandingOrder(["t2", "t1"]);

        phase.Update("Renamed", PhaseFormat.Custom, null, null, null);

        phase.Groups[0].ManualStandingOrder.Should().BeEquivalentTo(
            ["t2", "t1"], opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void CalculateGroupStandings_CustomFormat_UsesManualStandingOrder()
    {
        var phase = Phase.Create("Phase", PhaseFormat.Custom, order: 1, numberOfGroups: 1);
        var group = phase.Groups[0];
        group.AddTeam("t1");
        group.AddTeam("t2");
        group.AddTeam("t3");
        group.SetManualStandingOrder(["t3", "t1", "t2"]);

        var standings = phase.CalculateGroupStandings(group.Id, []);

        standings.Should().HaveCount(3);
        standings.Single(s => s.TeamId == "t3").Position.Should().Be(1);
        standings.Single(s => s.TeamId == "t1").Position.Should().Be(2);
        standings.Single(s => s.TeamId == "t2").Position.Should().Be(3);
    }
}
