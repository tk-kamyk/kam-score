using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;

namespace KamSquare.KamScore.Domain.UnitTest;

public class TournamentStructureTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var structure = TournamentStructure.Create("tournament-1");

        structure.TournamentId.Should().Be("tournament-1");
        structure.Phases.Should().BeEmpty();
        structure.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(structure.Id, out _).Should().BeTrue();
        structure.LastModified.Should().NotBeNull();
    }

    [Fact]
    public void AddPhase_ShouldAddPhaseWithCorrectOrder()
    {
        var structure = TournamentStructure.Create("tournament-1");

        var phase = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 2);

        phase.Name.Should().Be("Group Stage");
        phase.Format.Should().Be(PhaseFormat.RoundRobin);
        phase.Order.Should().Be(1);
        structure.Phases.Should().HaveCount(1);
    }

    [Fact]
    public void AddPhase_ShouldAutoCreateGroups()
    {
        var structure = TournamentStructure.Create("tournament-1");

        var phase = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 3);

        phase.Groups.Should().HaveCount(3);
        phase.Groups[0].Name.Should().Be("A");
        phase.Groups[1].Name.Should().Be("B");
        phase.Groups[2].Name.Should().Be("C");
        phase.Groups.Should().AllSatisfy(g =>
        {
            g.Id.Should().NotBeNullOrEmpty();
            g.TeamIds.Should().BeEmpty();
        });
    }

    [Fact]
    public void AddPhase_MultiplePhasesHaveSequentialOrder()
    {
        var structure = TournamentStructure.Create("tournament-1");

        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        structure.AddPhase("Playoffs", PhaseFormat.PlayoffElimination, 1);

        structure.Phases[0].Order.Should().Be(1);
        structure.Phases[1].Order.Should().Be(2);
    }

    [Fact]
    public void UpdatePhase_ShouldChangeNameAndFormat()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);

        structure.UpdatePhase(phase.Id, "Pool Stage", PhaseFormat.PlayoffWithPlacement);

        var updated = structure.GetPhase(phase.Id);
        updated.Name.Should().Be("Pool Stage");
        updated.Format.Should().Be(PhaseFormat.PlayoffWithPlacement);
    }

    [Fact]
    public void UpdatePhase_NonExistentPhase_ShouldThrowNotFoundException()
    {
        var structure = TournamentStructure.Create("tournament-1");

        var act = () => structure.UpdatePhase("nonexistent", "Name", PhaseFormat.RoundRobin);

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void RemovePhase_ShouldRemoveAndReorder()
    {
        var structure = TournamentStructure.Create("tournament-1");
        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var semis = structure.AddPhase("Semis", PhaseFormat.PlayoffElimination, 1);
        structure.AddPhase("Finals", PhaseFormat.PlayoffElimination, 1);

        structure.RemovePhase(semis.Id);

        structure.Phases.Should().HaveCount(2);
        structure.Phases[0].Name.Should().Be("Groups");
        structure.Phases[0].Order.Should().Be(1);
        structure.Phases[1].Name.Should().Be("Finals");
        structure.Phases[1].Order.Should().Be(2);
    }

    [Fact]
    public void RemovePhase_NonExistentPhase_ShouldThrowNotFoundException()
    {
        var structure = TournamentStructure.Create("tournament-1");

        var act = () => structure.RemovePhase("nonexistent");

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void AddGroup_ShouldAddGroupToPhase()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);

        var group = structure.AddGroup(phase.Id, "D");

        group.Name.Should().Be("D");
        group.Id.Should().NotBeNullOrEmpty();
        phase.Groups.Should().HaveCount(2);
    }

    [Fact]
    public void UpdateGroup_ShouldRenameGroup()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var groupId = phase.Groups[0].Id;

        structure.UpdateGroup(phase.Id, groupId, "Pool 1");

        structure.GetGroup(phase.Id, groupId).Name.Should().Be("Pool 1");
    }

    [Fact]
    public void RemoveGroup_ShouldRemoveGroupFromPhase()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var groupAId = phase.Groups[0].Id;

        structure.RemoveGroup(phase.Id, groupAId);

        phase.Groups.Should().HaveCount(1);
        phase.Groups[0].Name.Should().Be("B");
    }

    [Fact]
    public void AssignTeam_ShouldAddTeamIdToGroup()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var groupId = phase.Groups[0].Id;

        structure.AssignTeam(phase.Id, groupId, "team-1");

        phase.Groups[0].TeamIds.Should().Contain("team-1");
    }

    [Fact]
    public void RemoveTeam_ShouldRemoveTeamIdFromGroup()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var groupId = phase.Groups[0].Id;
        structure.AssignTeam(phase.Id, groupId, "team-1");

        structure.RemoveTeam(phase.Id, groupId, "team-1");

        phase.Groups[0].TeamIds.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTeam_NonExistentTeam_ShouldThrowNotFoundException()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var groupId = phase.Groups[0].Id;

        var act = () => structure.RemoveTeam(phase.Id, groupId, "nonexistent");

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void GroupNameExistsInPhase_ShouldReturnTrue_WhenNameExists()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);

        structure.GroupNameExistsInPhase(phase.Id, "A").Should().BeTrue();
        structure.GroupNameExistsInPhase(phase.Id, "a").Should().BeTrue();
    }

    [Fact]
    public void GroupNameExistsInPhase_ShouldReturnFalse_WhenExcludingSelf()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var groupId = phase.Groups[0].Id;

        structure.GroupNameExistsInPhase(phase.Id, "A", groupId).Should().BeFalse();
    }

    [Fact]
    public void TeamExistsInPhase_ShouldReturnTrue_WhenAssigned()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        structure.AssignTeam(phase.Id, phase.Groups[0].Id, "team-1");

        structure.TeamExistsInPhase(phase.Id, "team-1").Should().BeTrue();
    }

    [Fact]
    public void TeamExistsInPhase_ShouldReturnFalse_WhenNotAssigned()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);

        structure.TeamExistsInPhase(phase.Id, "team-1").Should().BeFalse();
    }

    [Fact]
    public void AddPhase_ShouldUpdateLastModified()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var initialModified = structure.LastModified;

        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);

        structure.LastModified.Should().BeOnOrAfter(initialModified!.Value);
    }
}
