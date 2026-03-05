using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Domain.UnitTest.Services;

public class PlaceholderResolverTests
{
    [Fact]
    public void Resolve_SwapsPlaceholderTeamIdsToRealTeamIds()
    {
        var placeholders = CreatePlaceholders("source-phase", 2);
        var nextPhase = CreatePhaseWithTeams(placeholders);
        var games = new List<Game>
        {
            Game.Create("t1", "p2", nextPhase.Groups[0].Id, 1,
                homeTeamId: placeholders[0].Id, awayTeamId: placeholders[1].Id)
        };

        var modified = PlaceholderResolver.Resolve(games, nextPhase, placeholders, ["real-a", "real-b"]);

        modified.Should().HaveCount(1);
        games[0].HomeTeamId.Should().Be("real-a");
        games[0].AwayTeamId.Should().Be("real-b");
    }

    [Fact]
    public void Resolve_SwapsGroupTeamIds()
    {
        var placeholders = CreatePlaceholders("source-phase", 2);
        var nextPhase = CreatePhaseWithTeams(placeholders);

        PlaceholderResolver.Resolve([], nextPhase, placeholders, ["real-a", "real-b"]);

        nextPhase.Groups[0].TeamIds.Should().Contain("real-a");
        nextPhase.Groups[0].TeamIds.Should().Contain("real-b");
        nextPhase.Groups[0].TeamIds.Should().NotContain(placeholders[0].Id);
        nextPhase.Groups[0].TeamIds.Should().NotContain(placeholders[1].Id);
    }

    [Fact]
    public void Resolve_SetsResolvedTeamIdOnPlaceholders()
    {
        var placeholders = CreatePlaceholders("source-phase", 2);
        var nextPhase = CreatePhaseWithTeams(placeholders);

        PlaceholderResolver.Resolve([], nextPhase, placeholders, ["real-a", "real-b"]);

        placeholders[0].ResolvedTeamId.Should().Be("real-a");
        placeholders[1].ResolvedTeamId.Should().Be("real-b");
    }

    [Fact]
    public void Resolve_SwapsRefereeTeamId()
    {
        var placeholders = CreatePlaceholders("source-phase", 3);
        var nextPhase = CreatePhaseWithTeams(placeholders);
        var games = new List<Game>
        {
            Game.Create("t1", "p2", nextPhase.Groups[0].Id, 1,
                homeTeamId: placeholders[0].Id, awayTeamId: placeholders[1].Id,
                refereeTeamId: placeholders[2].Id)
        };

        PlaceholderResolver.Resolve(games, nextPhase, placeholders, ["real-a", "real-b", "real-c"]);

        games[0].RefereeTeamId.Should().Be("real-c");
    }

    [Fact]
    public void Resolve_DoesNotModifyNonPlaceholderGames()
    {
        var placeholders = CreatePlaceholders("source-phase", 2);
        var nextPhase = CreatePhaseWithTeams(placeholders);
        var games = new List<Game>
        {
            Game.Create("t1", "p2", nextPhase.Groups[0].Id, 2,
                homeTeamPlaceholder: "Winner SF1", awayTeamPlaceholder: "Winner SF2")
        };

        var modified = PlaceholderResolver.Resolve(games, nextPhase, placeholders, ["real-a", "real-b"]);

        modified.Should().BeEmpty();
        games[0].HomeTeamPlaceholder.Should().Be("Winner SF1");
        games[0].AwayTeamPlaceholder.Should().Be("Winner SF2");
    }

    [Fact]
    public void Unresolve_SwapsRealTeamIdsBackToPlaceholderIds()
    {
        var placeholders = CreatePlaceholders("source-phase", 2);
        placeholders[0].ResolvedTeamId = "real-a";
        placeholders[1].ResolvedTeamId = "real-b";

        var nextPhase = Phase.Create("Playoffs", PhaseFormat.PlayoffElimination, 2, 1);
        nextPhase.Groups[0].AddTeam("real-a");
        nextPhase.Groups[0].AddTeam("real-b");

        var games = new List<Game>
        {
            Game.Create("t1", "p2", nextPhase.Groups[0].Id, 1,
                homeTeamId: "real-a", awayTeamId: "real-b")
        };

        var modified = PlaceholderResolver.Unresolve(games, nextPhase, placeholders);

        modified.Should().HaveCount(1);
        games[0].HomeTeamId.Should().Be(placeholders[0].Id);
        games[0].AwayTeamId.Should().Be(placeholders[1].Id);
    }

    [Fact]
    public void Unresolve_SwapsGroupTeamIdsBack()
    {
        var placeholders = CreatePlaceholders("source-phase", 2);
        placeholders[0].ResolvedTeamId = "real-a";
        placeholders[1].ResolvedTeamId = "real-b";

        var nextPhase = Phase.Create("Playoffs", PhaseFormat.PlayoffElimination, 2, 1);
        nextPhase.Groups[0].AddTeam("real-a");
        nextPhase.Groups[0].AddTeam("real-b");

        PlaceholderResolver.Unresolve([], nextPhase, placeholders);

        nextPhase.Groups[0].TeamIds.Should().Contain(placeholders[0].Id);
        nextPhase.Groups[0].TeamIds.Should().Contain(placeholders[1].Id);
    }

    [Fact]
    public void Unresolve_ClearsResolvedTeamId()
    {
        var placeholders = CreatePlaceholders("source-phase", 2);
        placeholders[0].ResolvedTeamId = "real-a";
        placeholders[1].ResolvedTeamId = "real-b";

        var nextPhase = Phase.Create("Playoffs", PhaseFormat.PlayoffElimination, 2, 1);

        PlaceholderResolver.Unresolve([], nextPhase, placeholders);

        placeholders[0].ResolvedTeamId.Should().BeNull();
        placeholders[1].ResolvedTeamId.Should().BeNull();
    }

    [Fact]
    public void Resolve_OrdersBySeeds()
    {
        // Create placeholders with seeds 1, 2 but in reverse order
        var placeholders = new List<Team>
        {
            Team.CreatePlaceholder("Seed 2", "t1", "source-phase", 2),
            Team.CreatePlaceholder("Seed 1", "t1", "source-phase", 1)
        };
        var nextPhase = CreatePhaseWithTeams(placeholders);
        var games = new List<Game>
        {
            Game.Create("t1", "p2", nextPhase.Groups[0].Id, 1,
                homeTeamId: placeholders[1].Id, // Seed 1
                awayTeamId: placeholders[0].Id)  // Seed 2
        };

        PlaceholderResolver.Resolve(games, nextPhase, placeholders, ["real-first", "real-second"]);

        // Seed 1 → real-first, Seed 2 → real-second
        games[0].HomeTeamId.Should().Be("real-first");
        games[0].AwayTeamId.Should().Be("real-second");
    }

    private static List<Team> CreatePlaceholders(string sourcePhaseId, int count)
    {
        return Enumerable.Range(1, count)
            .Select(seed => Team.CreatePlaceholder($"Phase - Seed {seed}", "t1", sourcePhaseId, seed))
            .ToList();
    }

    private static Phase CreatePhaseWithTeams(List<Team> teams)
    {
        var phase = Phase.Create("Playoffs", PhaseFormat.PlayoffElimination, 2, 1);
        foreach (var team in teams)
        {
            phase.Groups[0].AddTeam(team.Id);
        }

        return phase;
    }
}
