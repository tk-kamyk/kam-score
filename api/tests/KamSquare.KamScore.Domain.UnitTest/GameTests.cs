using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Domain.UnitTest;

public class GameTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var game = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "home", awayTeamId: "away",
            refereeTeamId: "ref");

        game.TournamentId.Should().Be("t1");
        game.PhaseId.Should().Be("p1");
        game.GroupId.Should().Be("g1");
        game.Round.Should().Be(1);
        game.HomeTeamId.Should().Be("home");
        game.AwayTeamId.Should().Be("away");
        game.RefereeTeamId.Should().Be("ref");
        game.Status.Should().Be(GameStatus.Scheduled);
        game.Id.Should().NotBeNullOrEmpty();
        game.LastModified.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithPlaceholders_ShouldSetPlaceholderFields()
    {
        var game = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2");

        game.HomeTeamId.Should().BeNull();
        game.AwayTeamId.Should().BeNull();
        game.HomeTeamPlaceholder.Should().Be("Winner SF1");
        game.AwayTeamPlaceholder.Should().Be("Winner SF2");
    }

    [Fact]
    public void AssignSchedule_ShouldSetCourtAndStartTime()
    {
        var game = Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b");
        var startTime = new DateTime(2026, 6, 1, 9, 0, 0);

        game.AssignSchedule("court1", startTime);

        game.CourtId.Should().Be("court1");
        game.StartTime.Should().Be(startTime);
        game.LastModified.Should().NotBeNull();
    }
}
