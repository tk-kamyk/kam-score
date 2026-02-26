using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.UnitTest;

public class TeamTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var team = Team.Create("Eagles", 75, "tournament-1", "eagles@example.com", "+123456789");

        team.Name.Should().Be("Eagles");
        team.Level.Should().Be(75);
        team.TournamentId.Should().Be("tournament-1");
        team.Email.Should().Be("eagles@example.com");
        team.Phone.Should().Be("+123456789");
    }

    [Fact]
    public void Create_ShouldGenerateValidGuidId()
    {
        var team = Team.Create("Eagles", 75, "tournament-1");

        team.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(team.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetLastModified()
    {
        var before = DateTime.UtcNow;
        var team = Team.Create("Eagles", 75, "tournament-1");
        var after = DateTime.UtcNow;

        team.LastModified.Should().NotBeNull();
        team.LastModified.Should().BeOnOrAfter(before);
        team.LastModified.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithOptionalFieldsNull_ShouldHaveNullEmailAndPhone()
    {
        var team = Team.Create("Eagles", 75, "tournament-1");

        team.Email.Should().BeNull();
        team.Phone.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldChangeAllProperties()
    {
        var team = Team.Create("Eagles", 75, "tournament-1", "eagles@example.com", "+123456789");

        team.Update("Hawks", 80, "hawks@example.com", "+987654321");

        team.Name.Should().Be("Hawks");
        team.Level.Should().Be(80);
        team.Email.Should().Be("hawks@example.com");
        team.Phone.Should().Be("+987654321");
    }

    [Fact]
    public void Update_ShouldUpdateLastModified()
    {
        var team = Team.Create("Eagles", 75, "tournament-1");
        var createdAt = team.LastModified;

        team.Update("Hawks", 80, null, null);

        team.LastModified.Should().BeOnOrAfter(createdAt!.Value);
    }

    [Fact]
    public void Update_ShouldNotChangeTournamentId()
    {
        var team = Team.Create("Eagles", 75, "tournament-1");

        team.Update("Hawks", 80, null, null);

        team.TournamentId.Should().Be("tournament-1");
    }
}
