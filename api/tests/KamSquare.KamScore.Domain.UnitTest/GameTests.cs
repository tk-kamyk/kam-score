using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

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

    [Fact]
    public void RecordResult_WithSets_ComputesScoresAndSetsCompleted()
    {
        var game = Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b");
        var sets = new List<SetResult>
        {
            new(25, 20),  // home wins
            new(23, 25),  // away wins
            new(15, 10)   // home wins
        };

        game.RecordResult(sets);

        game.HomeScore.Should().Be(2);
        game.AwayScore.Should().Be(1);
        game.Sets.Should().HaveCount(3);
        game.Status.Should().Be(GameStatus.Completed);
        game.LastModified.Should().NotBeNull();
    }

    [Fact]
    public void RecordResult_TiedSets_NotCountedForEither()
    {
        var game = Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b");
        var sets = new List<SetResult>
        {
            new(25, 20),  // home wins
            new(20, 20)   // tie (not counted)
        };

        game.RecordResult(sets);

        game.HomeScore.Should().Be(1);
        game.AwayScore.Should().Be(0);
    }

    [Fact]
    public void RecordSimpleResult_SetsScoreAndCompleted()
    {
        var game = Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b");

        game.RecordSimpleResult(2, 1);

        game.HomeScore.Should().Be(2);
        game.AwayScore.Should().Be(1);
        game.Sets.Should().BeNull();
        game.Status.Should().Be(GameStatus.Completed);
        game.LastModified.Should().NotBeNull();
    }

    [Fact]
    public void RecordSimpleResult_ZeroZero_SetsCompleted()
    {
        var game = Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b");

        game.RecordSimpleResult(0, 0);

        game.HomeScore.Should().Be(0);
        game.AwayScore.Should().Be(0);
        game.Status.Should().Be(GameStatus.Completed);
    }
}
