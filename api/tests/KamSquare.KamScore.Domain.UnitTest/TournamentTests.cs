using System.Text.RegularExpressions;
using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.UnitTest;

public class TournamentTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "user1");

        tournament.Name.Should().Be("Summer Cup");
        tournament.Discipline.Should().Be(Discipline.Volleyball);
        tournament.OwnerId.Should().Be("user1");
    }

    [Fact]
    public void Create_ShouldGenerateId()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "user1");

        tournament.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(tournament.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldGenerateTournamentCode()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "user1");

        tournament.TournamentCode.Should().NotBeNullOrEmpty();
        tournament.TournamentCode.Should().HaveLength(4);
        tournament.TournamentCode.Should().MatchRegex(@"^[0-9A-F]{4}$");
    }

    [Fact]
    public void GenerateTournamentCode_ShouldProduceValidFormat()
    {
        for (var i = 0; i < 100; i++)
        {
            var code = Tournament.GenerateTournamentCode();
            code.Should().MatchRegex(@"^[0-9A-F]{4}$");
        }
    }

    [Fact]
    public void IsOwnedBy_ShouldReturnTrue_ForOwner()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "user1");

        tournament.IsOwnedBy("user1").Should().BeTrue();
    }

    [Fact]
    public void IsOwnedBy_ShouldReturnFalse_ForOtherUser()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "user1");

        tournament.IsOwnedBy("user2").Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldChangeName()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "user1");

        tournament.Update("Winter Cup", Discipline.Volleyball, null, null, null);

        tournament.Name.Should().Be("Winter Cup");
    }

    [Fact]
    public void Update_ShouldChangeDiscipline()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "user1");

        tournament.Update("Summer Cup", Discipline.BeachVolleyball, null, null, null);

        tournament.Discipline.Should().Be(Discipline.BeachVolleyball);
    }

    [Fact]
    public void Update_ShouldSetGameConditions()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "user1");
        var conditions = new GameConditions(BestOfSets: 3, PointsPerSet: [25, 25, 15]);

        tournament.Update("Summer Cup", Discipline.Volleyball, null, 60, conditions);

        tournament.GameConditions.Should().NotBeNull();
        tournament.GameConditions!.BestOfSets.Should().Be(3);
        tournament.GameConditions.PointsPerSet.Should().BeEquivalentTo([25, 25, 15]);
        tournament.GameLength.Should().Be(60);
    }

    [Fact]
    public void Create_ShouldInitializeEmptyCollections()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "user1");

        tournament.Courts.Should().BeEmpty();
    }
}
