using FluentAssertions;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Domain.UnitTest;

public class RoundRobinGeneratorTests
{
    private const string TournamentId = "t1";
    private const string PhaseId = "p1";
    private const string GroupId = "g1";

    [Fact]
    public void Generate_WithEmptyTeams_ShouldReturnEmptyList()
    {
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, []);

        games.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithSingleTeam_ShouldReturnEmptyList()
    {
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, ["a"]);

        games.Should().BeEmpty();
    }

    [Fact]
    public void Generate_With2Teams_ShouldReturn1Game()
    {
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, ["a", "b"]);

        games.Should().HaveCount(1);
        games[0].Round.Should().Be(1);
        games[0].RefereeTeamId.Should().BeNull("no third team available to referee");
    }

    [Fact]
    public void Generate_With3Teams_ShouldReturn3Games()
    {
        var teams = new List<string> { "a", "b", "c" };
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, teams);

        games.Should().HaveCount(3);
        EachTeamPlaysNGames(games, teams, 2);
    }

    [Fact]
    public void Generate_With4Teams_ShouldReturn6Games()
    {
        var teams = new List<string> { "a", "b", "c", "d" };
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, teams);

        games.Should().HaveCount(6);
        EachTeamPlaysNGames(games, teams, 3);
    }

    [Fact]
    public void Generate_With5Teams_ShouldReturn10Games()
    {
        var teams = new List<string> { "a", "b", "c", "d", "e" };
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, teams);

        games.Should().HaveCount(10);
        EachTeamPlaysNGames(games, teams, 4);
    }

    [Fact]
    public void Generate_EachPairPlaysExactlyOnce()
    {
        var teams = new List<string> { "a", "b", "c", "d" };
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, teams);

        var pairs = games.Select(g =>
        {
            var sorted = new[] { g.HomeTeamId!, g.AwayTeamId! }.OrderBy(x => x).ToArray();
            return $"{sorted[0]}-{sorted[1]}";
        }).ToList();

        pairs.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Generate_With3Teams_AllGamesShouldHaveReferee()
    {
        var teams = new List<string> { "a", "b", "c" };
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, teams);

        games.Should().AllSatisfy(g => g.RefereeTeamId.Should().NotBeNull());
    }

    [Fact]
    public void Generate_With3Teams_RefereeIsIdleTeam()
    {
        var teams = new List<string> { "a", "b", "c" };
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, teams);

        foreach (var game in games)
        {
            game.RefereeTeamId.Should().NotBe(game.HomeTeamId);
            game.RefereeTeamId.Should().NotBe(game.AwayTeamId);
            teams.Should().Contain(game.RefereeTeamId!);
        }
    }

    [Fact]
    public void Generate_With4Teams_AllGamesShouldHaveReferee()
    {
        var teams = new List<string> { "a", "b", "c", "d" };
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, teams);

        games.Should().AllSatisfy(g =>
        {
            g.RefereeTeamId.Should().NotBeNull();
            g.RefereeTeamId.Should().NotBe(g.HomeTeamId);
            g.RefereeTeamId.Should().NotBe(g.AwayTeamId);
            teams.Should().Contain(g.RefereeTeamId!);
        });
    }

    [Fact]
    public void Generate_With4Teams_RefereeDistribution_ShouldBeBalanced()
    {
        var teams = new List<string> { "a", "b", "c", "d" };
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, teams);

        foreach (var team in teams)
        {
            var refCount = games.Count(g => g.RefereeTeamId == team);
            // 6 games, 4 teams → ~1-2 referee duties each
            refCount.Should().BeGreaterThanOrEqualTo(1);
            refCount.Should().BeLessThanOrEqualTo(2);
        }
    }

    [Fact]
    public void Generate_With6Teams_RefereeDistribution_ShouldBeBalanced()
    {
        var teams = new List<string> { "a", "b", "c", "d", "e", "f" };
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, teams);

        // 15 games, 6 teams → ~2-3 referee duties each
        games.Should().HaveCount(15);
        foreach (var team in teams)
        {
            var refCount = games.Count(g => g.RefereeTeamId == team);
            refCount.Should().BeGreaterThanOrEqualTo(2, $"team {team} should referee at least twice");
            refCount.Should().BeLessThanOrEqualTo(3, $"team {team} should referee at most 3 times");
        }
    }

    [Fact]
    public void Generate_With5Teams_SomeGamesHaveReferee()
    {
        var teams = new List<string> { "a", "b", "c", "d", "e" };
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, teams);

        // With 5 teams (padded to 6), 1 team has bye each round = potential referee
        games.Where(g => g.RefereeTeamId is not null).Should().NotBeEmpty();
    }

    [Fact]
    public void Generate_HomeAwayBalance_ShouldBeRoughlyEqual()
    {
        var teams = new List<string> { "a", "b", "c", "d" };
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, teams);

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
        var games = RoundRobinGenerator.Generate(TournamentId, PhaseId, GroupId, ["a", "b", "c"]);

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
