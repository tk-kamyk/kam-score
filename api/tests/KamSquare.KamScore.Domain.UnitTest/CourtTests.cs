using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.UnitTest;

public class CourtTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var court = Court.Create("Court A", "tournament-1");

        court.Name.Should().Be("Court A");
        court.TournamentId.Should().Be("tournament-1");
    }

    [Fact]
    public void Create_ShouldGenerateValidGuidId()
    {
        var court = Court.Create("Court A", "tournament-1");

        court.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(court.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetLastModified()
    {
        var before = DateTime.UtcNow;
        var court = Court.Create("Court A", "tournament-1");
        var after = DateTime.UtcNow;

        court.LastModified.Should().NotBeNull();
        court.LastModified.Should().BeOnOrAfter(before);
        court.LastModified.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Update_ShouldChangeName()
    {
        var court = Court.Create("Court A", "tournament-1");

        court.Update("Main Court");

        court.Name.Should().Be("Main Court");
    }

    [Fact]
    public void Update_ShouldUpdateLastModified()
    {
        var court = Court.Create("Court A", "tournament-1");
        var createdAt = court.LastModified;

        court.Update("Main Court");

        court.LastModified.Should().BeOnOrAfter(createdAt!.Value);
    }

    [Fact]
    public void Update_ShouldNotChangeTournamentId()
    {
        var court = Court.Create("Court A", "tournament-1");

        court.Update("Main Court");

        court.TournamentId.Should().Be("tournament-1");
    }
}
