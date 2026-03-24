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
        var team = Team.Create("Eagles", 75, "tournament-1");

        team.LastModified.Should().NotBeNull();
        team.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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

    [Fact]
    public void GenerateSeedTeams_ShouldCreateTeamsWithSeedNames()
    {
        var teams = Team.GenerateSeedTeams(3, 1, "tournament-1");

        teams.Should().HaveCount(3);
        teams[0].Name.Should().Be("Seed 1");
        teams[1].Name.Should().Be("Seed 2");
        teams[2].Name.Should().Be("Seed 3");
    }

    [Fact]
    public void GenerateSeedTeams_ShouldUseStartIndexForNaming()
    {
        var teams = Team.GenerateSeedTeams(2, 5, "tournament-1");

        teams[0].Name.Should().Be("Seed 5");
        teams[1].Name.Should().Be("Seed 6");
    }

    [Fact]
    public void GenerateSeedTeams_FourTeams_ShouldDistributeLevelsProportionally()
    {
        var teams = Team.GenerateSeedTeams(4, 1, "tournament-1");

        teams[0].Level.Should().Be(0);
        teams[1].Level.Should().Be(33);
        teams[2].Level.Should().Be(67);
        teams[3].Level.Should().Be(100);
    }

    [Fact]
    public void GenerateSeedTeams_SingleTeam_ShouldAssignLevel50()
    {
        var teams = Team.GenerateSeedTeams(1, 1, "tournament-1");

        teams.Should().HaveCount(1);
        teams[0].Level.Should().Be(50);
    }

    [Fact]
    public void GenerateSeedTeams_ShouldCreateRealTeamsNotPlaceholders()
    {
        var teams = Team.GenerateSeedTeams(3, 1, "tournament-1");

        teams.Should().AllSatisfy(t =>
        {
            t.IsPlaceholder.Should().BeFalse();
            t.SourcePhaseId.Should().BeNull();
            t.Seed.Should().BeNull();
            t.TournamentId.Should().Be("tournament-1");
            t.Id.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void GenerateSeedTeams_TwoTeams_ShouldDistributeLevels0And100()
    {
        var teams = Team.GenerateSeedTeams(2, 1, "tournament-1");

        teams[0].Level.Should().Be(0);
        teams[1].Level.Should().Be(100);
    }
}
