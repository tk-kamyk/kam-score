using FluentAssertions;
using KamSquare.KamScore.Domain.Services.Formats;

namespace KamSquare.KamScore.Domain.UnitTest;

public class RoundRobinGeneratorTests
{
    private const string TournamentId = "t1";
    private const string PhaseId = "p1";
    private const string GroupId = "g1";

    private readonly IPhaseFormatStrategy _strategy = new RoundRobinStrategy();

    [Fact]
    public void Generate_WithEmptyTeams_ShouldReturnEmptyList()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, []);

        games.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithSingleTeam_ShouldReturnEmptyList()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, ["a"]);

        games.Should().BeEmpty();
    }

    [Fact]
    public void Generate_With2Teams_ShouldReturn1Game()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, ["a", "b"]);

        games.Should().HaveCount(1);
        games[0].Round.Should().Be(1);
        games[0].RefereeTeamId.Should().BeNull("referees are assigned after scheduling");
    }

    [Fact]
    public void Generate_With3Teams_ShouldReturn3Games()
    {
        var teams = new List<string> { "a", "b", "c" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().HaveCount(3);
        EachTeamPlaysNGames(games, teams, 2);
    }

    [Fact]
    public void Generate_With4Teams_ShouldReturn6Games()
    {
        var teams = new List<string> { "a", "b", "c", "d" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().HaveCount(6);
        EachTeamPlaysNGames(games, teams, 3);
    }

    [Fact]
    public void Generate_With5Teams_ShouldReturn10Games()
    {
        var teams = new List<string> { "a", "b", "c", "d", "e" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().HaveCount(10);
        EachTeamPlaysNGames(games, teams, 4);
    }

    [Fact]
    public void Generate_EachPairPlaysExactlyOnce()
    {
        var teams = new List<string> { "a", "b", "c", "d" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var pairs = games.Select(g =>
        {
            var sorted = new[] { g.HomeTeamId!, g.AwayTeamId! }.OrderBy(x => x).ToArray();
            return $"{sorted[0]}-{sorted[1]}";
        }).ToList();

        pairs.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Generate_NoRefereesAssigned()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, ["a", "b", "c", "d"]);

        games.Should().AllSatisfy(g =>
            g.RefereeTeamId.Should().BeNull("referees are assigned after scheduling by RefereeAssigner"));
    }

    [Fact]
    public void Generate_HomeAwayBalance_ShouldBeRoughlyEqual()
    {
        var teams = new List<string> { "a", "b", "c", "d" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        foreach (var team in teams)
        {
            var homeCount = games.Count(g => g.HomeTeamId == team);
            var awayCount = games.Count(g => g.AwayTeamId == team);

            // Each team plays 3 games, home/away diff should be at most 1
            Math.Abs(homeCount - awayCount).Should().BeLessThanOrEqualTo(1);
        }
    }

    [Fact]
    public void Generate_SetsCorrectTournamentPhaseGroupIds()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, ["a", "b", "c"]);

        games.Should().AllSatisfy(g =>
        {
            g.TournamentId.Should().Be(TournamentId);
            g.PhaseId.Should().Be(PhaseId);
            g.GroupId.Should().Be(GroupId);
        });
    }

    private static void EachTeamPlaysNGames(List<Entities.Game> games, List<string> teams, int expectedGames)
    {
        foreach (var team in teams)
        {
            var count = games.Count(g => g.HomeTeamId == team || g.AwayTeamId == team);
            count.Should().Be(expectedGames, $"team {team} should play {expectedGames} games");
        }
    }
}
