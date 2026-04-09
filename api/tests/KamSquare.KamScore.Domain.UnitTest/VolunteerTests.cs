using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.UnitTest;

public class VolunteerTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1", "john@email.com", "team-1");

        volunteer.Name.Should().Be("John Doe");
        volunteer.TournamentId.Should().Be("tournament-1");
        volunteer.Contact.Should().Be("john@email.com");
        volunteer.TeamId.Should().Be("team-1");
    }

    [Fact]
    public void Create_ShouldGenerateValidGuidId()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");

        volunteer.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(volunteer.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetLastModified()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");

        volunteer.LastModified.Should().NotBeNull();
        volunteer.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithOptionalFieldsNull_ShouldSetNulls()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");

        volunteer.Contact.Should().BeNull();
        volunteer.TeamId.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldChangeAllFields()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1", "old@email.com", "team-1");

        volunteer.Update("Jane Smith", "new@email.com", "team-2");

        volunteer.Name.Should().Be("Jane Smith");
        volunteer.Contact.Should().Be("new@email.com");
        volunteer.TeamId.Should().Be("team-2");
    }

    [Fact]
    public void Update_ShouldUpdateLastModified()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");
        var createdAt = volunteer.LastModified;

        volunteer.Update("Jane Smith", null, null);

        volunteer.LastModified.Should().BeOnOrAfter(createdAt!.Value);
    }

    [Fact]
    public void Update_ShouldNotChangeTournamentId()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");

        volunteer.Update("Jane Smith", "contact", "team-2");

        volunteer.TournamentId.Should().Be("tournament-1");
    }

    [Fact]
    public void Update_ShouldAllowClearingOptionalFields()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1", "john@email.com", "team-1");

        volunteer.Update("John Doe", null, null);

        volunteer.Contact.Should().BeNull();
        volunteer.TeamId.Should().BeNull();
    }

    // --- Shift Assignment ---

    [Fact]
    public void AssignShift_ShouldAddToAssignments()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");

        volunteer.AssignShift("Pool", new TimeOnly(9, 0));

        volunteer.Assignments.Should().ContainSingle()
            .Which.Should().Be(new ShiftAssignment("Pool", new TimeOnly(9, 0)));
    }

    [Fact]
    public void AssignShift_SpecialShift_ShouldAddWithNullTime()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");

        volunteer.AssignShift("Set-up", null);

        volunteer.Assignments.Should().ContainSingle()
            .Which.Should().Be(new ShiftAssignment("Set-up", null));
    }

    [Fact]
    public void AssignShift_DuplicateAssignment_ShouldNotAddTwice()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");

        volunteer.AssignShift("Pool", new TimeOnly(9, 0));
        volunteer.AssignShift("Pool", new TimeOnly(9, 0));

        volunteer.Assignments.Should().HaveCount(1);
    }

    [Fact]
    public void AssignShift_MultipleShifts_ShouldTrackAll()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");

        volunteer.AssignShift("Pool", new TimeOnly(9, 0));
        volunteer.AssignShift("Pool", new TimeOnly(9, 20));
        volunteer.AssignShift("Cleanup", null);

        volunteer.Assignments.Should().HaveCount(3);
    }

    [Fact]
    public void UnassignShift_ShouldRemoveFromAssignments()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");
        volunteer.AssignShift("Pool", new TimeOnly(9, 0));
        volunteer.AssignShift("Pool", new TimeOnly(9, 20));

        volunteer.UnassignShift("Pool", new TimeOnly(9, 0));

        volunteer.Assignments.Should().ContainSingle()
            .Which.Should().Be(new ShiftAssignment("Pool", new TimeOnly(9, 20)));
    }

    [Fact]
    public void UnassignShift_SpecialShift_ShouldRemove()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");
        volunteer.AssignShift("Set-up", null);

        volunteer.UnassignShift("Set-up", null);

        volunteer.Assignments.Should().BeEmpty();
    }

    [Fact]
    public void UnassignShift_NonExistentAssignment_ShouldNotThrow()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");

        var action = () => volunteer.UnassignShift("Pool", new TimeOnly(9, 0));

        action.Should().NotThrow();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyAssignments()
    {
        var volunteer = Volunteer.Create("John Doe", "tournament-1");

        volunteer.Assignments.Should().BeEmpty();
    }
}
