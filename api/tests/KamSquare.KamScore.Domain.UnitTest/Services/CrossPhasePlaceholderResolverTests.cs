using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Domain.UnitTest.Services;

public class CrossPhasePlaceholderResolverTests
{
    [Fact]
    public void Resolve_ReplacesPlaceholdersWithRealTeamIds()
    {
        var games = new List<Game>
        {
            Game.Create("t1", "p2", "g1", 1,
                homeTeamPlaceholder: "Group Stage - Seed 1",
                awayTeamPlaceholder: "Group Stage - Seed 2")
        };
        var seedMapping = new Dictionary<int, string>
        {
            { 1, "team-a" },
            { 2, "team-b" }
        };

        var modified = CrossPhasePlaceholderResolver.Resolve(games, seedMapping, "Group Stage");

        modified.Should().HaveCount(1);
        games[0].HomeTeamId.Should().Be("team-a");
        games[0].AwayTeamId.Should().Be("team-b");
    }

    [Fact]
    public void Resolve_KeepsPlaceholderStringsIntact()
    {
        var games = new List<Game>
        {
            Game.Create("t1", "p2", "g1", 1,
                homeTeamPlaceholder: "Group Stage - Seed 1",
                awayTeamPlaceholder: "Group Stage - Seed 2")
        };
        var seedMapping = new Dictionary<int, string>
        {
            { 1, "team-a" },
            { 2, "team-b" }
        };

        CrossPhasePlaceholderResolver.Resolve(games, seedMapping, "Group Stage");

        games[0].HomeTeamPlaceholder.Should().Be("Group Stage - Seed 1");
        games[0].AwayTeamPlaceholder.Should().Be("Group Stage - Seed 2");
    }

    [Fact]
    public void Resolve_OnlyMatchesCorrectSourcePhaseName()
    {
        var games = new List<Game>
        {
            Game.Create("t1", "p2", "g1", 1,
                homeTeamPlaceholder: "Other Phase - Seed 1",
                awayTeamPlaceholder: "Group Stage - Seed 1")
        };
        var seedMapping = new Dictionary<int, string> { { 1, "team-a" } };

        var modified = CrossPhasePlaceholderResolver.Resolve(games, seedMapping, "Group Stage");

        modified.Should().HaveCount(1);
        games[0].HomeTeamId.Should().BeNull();  // "Other Phase" not matched
        games[0].AwayTeamId.Should().Be("team-a");
    }

    [Fact]
    public void Resolve_DoesNotModifyWithinPhasePlaceholders()
    {
        var games = new List<Game>
        {
            Game.Create("t1", "p2", "g1", 2,
                homeTeamPlaceholder: "Winner SF1",
                awayTeamPlaceholder: "Winner SF2")
        };
        var seedMapping = new Dictionary<int, string> { { 1, "team-a" } };

        var modified = CrossPhasePlaceholderResolver.Resolve(games, seedMapping, "Group Stage");

        modified.Should().BeEmpty();
        games[0].HomeTeamId.Should().BeNull();
        games[0].AwayTeamId.Should().BeNull();
    }

    [Fact]
    public void Unresolve_ClearsTeamIdsButKeepsPlaceholders()
    {
        var games = new List<Game>
        {
            Game.Create("t1", "p2", "g1", 1,
                homeTeamId: "team-a", awayTeamId: "team-b",
                homeTeamPlaceholder: "Group Stage - Seed 1",
                awayTeamPlaceholder: "Group Stage - Seed 2")
        };

        var modified = CrossPhasePlaceholderResolver.Unresolve(games, "Group Stage");

        modified.Should().HaveCount(1);
        games[0].HomeTeamId.Should().BeNull();
        games[0].AwayTeamId.Should().BeNull();
        games[0].HomeTeamPlaceholder.Should().Be("Group Stage - Seed 1");
        games[0].AwayTeamPlaceholder.Should().Be("Group Stage - Seed 2");
    }

    [Fact]
    public void BuildSeedMapping_ProducesCorrectMapping()
    {
        var seededTeamIds = new List<string> { "team-a", "team-b", "team-c" };

        var mapping = CrossPhasePlaceholderResolver.BuildSeedMapping(seededTeamIds);

        mapping.Should().HaveCount(3);
        mapping[1].Should().Be("team-a");
        mapping[2].Should().Be("team-b");
        mapping[3].Should().Be("team-c");
    }
}
