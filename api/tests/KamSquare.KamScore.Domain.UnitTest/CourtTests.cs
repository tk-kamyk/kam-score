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
        var court = Court.Create("Court A", "tournament-1");

        court.LastModified.Should().NotBeNull();
        court.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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

    [Fact]
    public void GenerateCourts_ShouldCreateCourtsWithCNames()
    {
        var courts = Court.GenerateCourts(3, 1, "tournament-1");

        courts.Should().HaveCount(3);
        courts[0].Name.Should().Be("C1");
        courts[1].Name.Should().Be("C2");
        courts[2].Name.Should().Be("C3");
    }

    [Fact]
    public void GenerateCourts_ShouldUseStartIndexForNaming()
    {
        var courts = Court.GenerateCourts(2, 5, "tournament-1");

        courts[0].Name.Should().Be("C5");
        courts[1].Name.Should().Be("C6");
    }

    [Fact]
    public void GenerateCourts_ShouldSetTournamentIdAndGenerateIds()
    {
        var courts = Court.GenerateCourts(3, 1, "tournament-1");

        courts.Should().AllSatisfy(c =>
        {
            c.TournamentId.Should().Be("tournament-1");
            c.Id.Should().NotBeNullOrEmpty();
            Guid.TryParse(c.Id, out _).Should().BeTrue();
        });
    }
}
