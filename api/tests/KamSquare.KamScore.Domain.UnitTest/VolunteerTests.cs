using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;

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
}
