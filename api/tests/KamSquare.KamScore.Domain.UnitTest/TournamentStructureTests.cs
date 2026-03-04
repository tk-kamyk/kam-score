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
    public void AutoAssignTeams_EvenDistribution_ShouldDistributeEvenly()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 3);
        var teamIds = Enumerable.Range(1, 6).Select(i => $"team-{i}").ToList();

        structure.AutoAssignTeams(phase.Id, teamIds);

        phase.Groups[0].TeamIds.Should().HaveCount(2);
        phase.Groups[1].TeamIds.Should().HaveCount(2);
        phase.Groups[2].TeamIds.Should().HaveCount(2);
    }

    [Fact]
    public void AutoAssignTeams_UnevenDistribution_ShouldDistributeCorrectly()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 3);
        var teamIds = Enumerable.Range(1, 7).Select(i => $"team-{i}").ToList();

        structure.AutoAssignTeams(phase.Id, teamIds);

        phase.Groups[0].TeamIds.Should().HaveCount(3);
        phase.Groups[1].TeamIds.Should().HaveCount(2);
        phase.Groups[2].TeamIds.Should().HaveCount(2);
    }

    [Fact]
    public void AutoAssignTeams_ShouldFollowSnakeOrder()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 3);
        var teamIds = Enumerable.Range(1, 9).Select(i => $"team-{i}").ToList();

        structure.AutoAssignTeams(phase.Id, teamIds);

        // Round 1 (L→R): team-1→A, team-2→B, team-3→C
        // Round 2 (R→L): team-4→C, team-5→B, team-6→A
        // Round 3 (L→R): team-7→A, team-8→B, team-9→C
        phase.Groups[0].TeamIds.Should().Equal("team-1", "team-6", "team-7");
        phase.Groups[1].TeamIds.Should().Equal("team-2", "team-5", "team-8");
        phase.Groups[2].TeamIds.Should().Equal("team-3", "team-4", "team-9");
    }

    [Fact]
    public void AutoAssignTeams_ShouldClearExistingAssignments()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        structure.AssignTeam(phase.Id, phase.Groups[0].Id, "old-team-1");
        structure.AssignTeam(phase.Id, phase.Groups[1].Id, "old-team-2");

        structure.AutoAssignTeams(phase.Id, ["new-team-1", "new-team-2"]);

        phase.Groups[0].TeamIds.Should().NotContain("old-team-1");
        phase.Groups[1].TeamIds.Should().NotContain("old-team-2");
        phase.Groups.SelectMany(g => g.TeamIds).Should().BeEquivalentTo(["new-team-1", "new-team-2"]);
    }

    [Fact]
    public void AutoAssignTeams_EmptyTeamsList_ShouldClearAllGroups()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        structure.AssignTeam(phase.Id, phase.Groups[0].Id, "team-1");

        structure.AutoAssignTeams(phase.Id, new List<string>());

        phase.Groups.Should().AllSatisfy(g => g.TeamIds.Should().BeEmpty());
    }

    [Fact]
    public void AutoAssignTeams_NonExistentPhase_ShouldThrowNotFoundException()
    {
        var structure = TournamentStructure.Create("tournament-1");

        var act = () => structure.AutoAssignTeams("nonexistent", ["team-1"]);

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void AddPhase_WithProgressionFields_ShouldStoreValues()
    {
        var structure = TournamentStructure.Create("tournament-1");

        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2,
            groupWinners: 2, totalTeamsProceeding: 6);

        phase.GroupWinners.Should().Be(2);
        phase.TotalTeamsProceeding.Should().Be(6);
    }

    [Fact]
    public void AddPhase_WithoutProgressionFields_ShouldDefaultToNull()
    {
        var structure = TournamentStructure.Create("tournament-1");

        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);

        phase.GroupWinners.Should().BeNull();
        phase.TotalTeamsProceeding.Should().BeNull();
    }

    [Fact]
    public void UpdatePhase_ShouldChangeProgressionFields()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);

        structure.UpdatePhase(phase.Id, "Groups", PhaseFormat.RoundRobin,
            groupWinners: 1, totalTeamsProceeding: 4);

        var updated = structure.GetPhase(phase.Id);
        updated.GroupWinners.Should().Be(1);
        updated.TotalTeamsProceeding.Should().Be(4);
    }

    [Fact]
    public void AddPhase_WithStartTime_ShouldStoreValue()
    {
        var structure = TournamentStructure.Create("tournament-1");

        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2,
            startTime: new TimeOnly(9, 30));

        phase.StartTime.Should().Be(new TimeOnly(9, 30));
    }

    [Fact]
    public void UpdatePhase_ShouldChangeStartTime()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);

        structure.UpdatePhase(phase.Id, "Groups", PhaseFormat.RoundRobin,
            startTime: new TimeOnly(14, 0));

        var updated = structure.GetPhase(phase.Id);
        updated.StartTime.Should().Be(new TimeOnly(14, 0));
    }

    [Fact]
    public void AddPhase_ShouldUpdateLastModified()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var initialModified = structure.LastModified;

        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);

        structure.LastModified.Should().BeOnOrAfter(initialModified!.Value);
    }

    // --- Phase Status Lifecycle ---

    [Fact]
    public void AddPhase_ShouldHaveStatusNew()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);

        phase.Status.Should().Be(PhaseStatus.New);
    }

    [Fact]
    public void ActivatePhase_ShouldSetStatusToInProgress()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);

        structure.ActivatePhase(phase.Id);

        phase.Status.Should().Be(PhaseStatus.InProgress);
    }

    [Fact]
    public void CompletePhase_ShouldSetStatusToCompleted()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        structure.ActivatePhase(phase.Id);

        structure.CompletePhase(phase.Id);

        phase.Status.Should().Be(PhaseStatus.Completed);
    }

    [Fact]
    public void ReopenPhase_ShouldSetStatusToInProgress()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        structure.ActivatePhase(phase.Id);
        structure.CompletePhase(phase.Id);

        structure.ReopenPhase(phase.Id);

        phase.Status.Should().Be(PhaseStatus.InProgress);
    }

    [Fact]
    public void ReopenPhase_ShouldClearNextPhaseTeamsAndRevertToNew()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase1 = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var phase2 = structure.AddPhase("Playoffs", PhaseFormat.PlayoffElimination, 1);
        structure.ActivatePhase(phase1.Id);
        structure.CompletePhase(phase1.Id);
        structure.AutoAssignTeams(phase2.Id, ["t1", "t2", "t3", "t4"]);
        structure.ActivatePhase(phase2.Id);

        structure.ReopenPhase(phase1.Id);

        phase2.Status.Should().Be(PhaseStatus.New);
        phase2.Groups.Should().AllSatisfy(g => g.TeamIds.Should().BeEmpty());
    }

    [Fact]
    public void GetNextPhase_ShouldReturnNextPhaseByOrder()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase1 = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var phase2 = structure.AddPhase("Playoffs", PhaseFormat.PlayoffElimination, 1);

        var next = structure.GetNextPhase(phase1.Id);

        next.Should().NotBeNull();
        next!.Id.Should().Be(phase2.Id);
    }

    [Fact]
    public void GetNextPhase_ShouldReturnNull_WhenLastPhase()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);

        var next = structure.GetNextPhase(phase.Id);

        next.Should().BeNull();
    }

    [Fact]
    public void GetPreviousPhase_ShouldReturnPreviousPhaseByOrder()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase1 = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var phase2 = structure.AddPhase("Playoffs", PhaseFormat.PlayoffElimination, 1);

        var prev = structure.GetPreviousPhase(phase2.Id);

        prev.Should().NotBeNull();
        prev!.Id.Should().Be(phase1.Id);
    }

    [Fact]
    public void GetPreviousPhase_ShouldReturnNull_WhenFirstPhase()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);

        var prev = structure.GetPreviousPhase(phase.Id);

        prev.Should().BeNull();
    }
}
