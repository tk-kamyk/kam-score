using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Domain.UnitTest.Services;

public class CrossPhaseGameGeneratorTests
{
    [Fact]
    public void GenerateWithPlaceholders_RoundRobin_CreatesPlaceholderGames()
    {
        var phase = Phase.Create("Playoffs", PhaseFormat.RoundRobin, 2, 1);

        var games = CrossPhaseGameGenerator.GenerateWithPlaceholders(
            "t1", phase.Id, phase, "Group Stage", 4);

        games.Should().NotBeEmpty();
        games.Should().AllSatisfy(g =>
        {
            // Team IDs should be null for cross-phase placeholder games
            g.HomeTeamId.Should().BeNull();
            g.AwayTeamId.Should().BeNull();

            // Placeholder strings should contain source phase name and seed number
            g.HomeTeamPlaceholder.Should().StartWith("Group Stage - Seed ");
            g.AwayTeamPlaceholder.Should().StartWith("Group Stage - Seed ");

            // No referee for placeholder games
            g.RefereeTeamId.Should().BeNull();
        });
    }

    [Fact]
    public void GenerateWithPlaceholders_RoundRobin_UsesCorrectSeedDistribution()
    {
        var phase = Phase.Create("Round 2", PhaseFormat.RoundRobin, 2, 2);

        var games = CrossPhaseGameGenerator.GenerateWithPlaceholders(
            "t1", phase.Id, phase, "Group Stage", 8);

        // With 8 teams, 2 groups: snake draft gives Group A: seeds 1,4,5,8; Group B: seeds 2,3,6,7
        var groupAGames = games.Where(g => g.GroupId == phase.Groups[0].Id).ToList();
        var groupBGames = games.Where(g => g.GroupId == phase.Groups[1].Id).ToList();

        // Each group of 4 teams should produce 6 round-robin games (4C2)
        groupAGames.Should().HaveCount(6);
        groupBGames.Should().HaveCount(6);

        // Check that Group A contains seeds 1, 4, 5, 8
        var groupAPlaceholders = groupAGames
            .SelectMany(g => new[] { g.HomeTeamPlaceholder, g.AwayTeamPlaceholder })
            .Where(p => p is not null)
            .Distinct()
            .ToList();

        groupAPlaceholders.Should().Contain("Group Stage - Seed 1");
        groupAPlaceholders.Should().Contain("Group Stage - Seed 4");
        groupAPlaceholders.Should().Contain("Group Stage - Seed 5");
        groupAPlaceholders.Should().Contain("Group Stage - Seed 8");
    }

    [Fact]
    public void GenerateWithPlaceholders_PlayoffElimination_FirstRoundHasCrossPhase_LaterRoundsHaveWinner()
    {
        var phase = Phase.Create("Playoffs", PhaseFormat.PlayoffElimination, 2, 1);

        var games = CrossPhaseGameGenerator.GenerateWithPlaceholders(
            "t1", phase.Id, phase, "Group Stage", 4);

        // 4 teams, single group → SF1, SF2, Final
        var firstRoundGames = games.Where(g => g.Round == 1).ToList();
        var laterRoundGames = games.Where(g => g.Round > 1).ToList();

        // First round: cross-phase placeholders
        firstRoundGames.Should().NotBeEmpty();
        firstRoundGames.Should().AllSatisfy(g =>
        {
            g.HomeTeamId.Should().BeNull();
            g.AwayTeamId.Should().BeNull();
        });

        // Later rounds: "Winner" placeholders (within-phase bracket advancement)
        laterRoundGames.Should().NotBeEmpty();
        laterRoundGames.Should().OnlyContain(g =>
            (g.HomeTeamPlaceholder != null && g.HomeTeamPlaceholder.StartsWith("Winner")) ||
            (g.AwayTeamPlaceholder != null && g.AwayTeamPlaceholder.StartsWith("Winner")) ||
            g.HomeTeamId != null || g.AwayTeamId != null);
    }

    [Fact]
    public void GenerateWithPlaceholders_PlayoffWithPlacement_GeneratesGames()
    {
        var phase = Phase.Create("Playoffs", PhaseFormat.PlayoffWithPlacement, 2, 1);

        var games = CrossPhaseGameGenerator.GenerateWithPlaceholders(
            "t1", phase.Id, phase, "Group Stage", 4);

        // 4 teams → SF + placement games
        games.Should().NotBeEmpty();
        // First round should have cross-phase placeholders
        var firstRoundGames = games.Where(g => g.Round == 1).ToList();
        firstRoundGames.Should().AllSatisfy(g =>
        {
            g.HomeTeamId.Should().BeNull();
            g.AwayTeamId.Should().BeNull();
        });
    }

    [Fact]
    public void FormatPlaceholder_ProducesExpectedFormat()
    {
        var result = CrossPhaseGameGenerator.FormatPlaceholder("Group Stage", 3);

        result.Should().Be("Group Stage - Seed 3");
    }
}
