using FluentAssertions;
using KamSquare.KamScore.Domain.Services.Formats;

namespace KamSquare.KamScore.Domain.UnitTest;

public class CustomGeneratorTests
{
    private const string TournamentId = "t1";
    private const string PhaseId = "p1";
    private const string GroupId = "g1";

    private readonly IPhaseFormatStrategy _strategy = new CustomStrategy();

    [Fact]
    public void Generate_WithEmptyTeams_ShouldReturnEmptyList()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, []);

        games.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithMultipleTeams_StillReturnsEmptyList()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, ["a", "b", "c", "d"]);

        games.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTeams_ShouldNotThrow_ForAnyTeamCount()
    {
        var act = () => _strategy.ValidateTeams([]);

        act.Should().NotThrow();
    }

    [Fact]
    public void SupportsRefereeAssignment_ShouldBeFalse()
    {
        _strategy.SupportsRefereeAssignment.Should().BeFalse();
    }
}
