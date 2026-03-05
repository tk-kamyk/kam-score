using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Domain.UnitTest.Services;

public class PlaceholderTeamGeneratorTests
{
    [Fact]
    public void Generate_WithTotalTeamsProceeding_CreatesCorrectCount()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2, totalTeamsProceeding: 4);

        var result = PlaceholderTeamGenerator.Generate(phase, "t1");

        result.Should().NotBeNull();
        result.Should().HaveCount(4);
    }

    [Fact]
    public void Generate_WithGroupWinners_CreatesCorrectCount()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2, groupWinners: 2);

        var result = PlaceholderTeamGenerator.Generate(phase, "t1");

        result.Should().NotBeNull();
        result.Should().HaveCount(4); // 2 winners × 2 groups
    }

    [Fact]
    public void Generate_WithBothConfig_UsesTotalTeamsProceeding()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2, groupWinners: 1, totalTeamsProceeding: 6);

        var result = PlaceholderTeamGenerator.Generate(phase, "t1");

        result.Should().NotBeNull();
        result.Should().HaveCount(6);
    }

    [Fact]
    public void Generate_WithNoConfig_ReturnsNull()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2);

        var result = PlaceholderTeamGenerator.Generate(phase, "t1");

        result.Should().BeNull();
    }

    [Fact]
    public void Generate_SetsCorrectProperties()
    {
        var phase = Phase.Create("Group Stage", PhaseFormat.RoundRobin, 1, 2, totalTeamsProceeding: 3);

        var result = PlaceholderTeamGenerator.Generate(phase, "t1")!;

        result.Should().AllSatisfy(t =>
        {
            t.IsPlaceholder.Should().BeTrue();
            t.SourcePhaseId.Should().Be(phase.Id);
            t.TournamentId.Should().Be("t1");
            t.Id.Should().NotBeNullOrEmpty();
        });

        result[0].Seed.Should().Be(1);
        result[0].Name.Should().Be("Group Stage - Seed 1");
        result[1].Seed.Should().Be(2);
        result[1].Name.Should().Be("Group Stage - Seed 2");
        result[2].Seed.Should().Be(3);
        result[2].Name.Should().Be("Group Stage - Seed 3");
    }

    [Fact]
    public void FormatPlaceholderName_ProducesExpectedFormat()
    {
        var result = PlaceholderTeamGenerator.FormatPlaceholderName("Pool Play", 5);

        result.Should().Be("Pool Play - Seed 5");
    }
}
