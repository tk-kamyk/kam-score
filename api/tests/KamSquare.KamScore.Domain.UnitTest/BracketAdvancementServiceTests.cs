using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Domain.UnitTest;

public class BracketAdvancementServiceTests
{
    [Fact]
    public void ResolveAdvancement_WinnerPlaceholder_ResolvesHomeTeam()
    {
        var sf1 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks", label: "SF1");
        sf1.RecordSimpleResult(2, 1);

        var final_ = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2",
            label: "Final");

        var modified = BracketAdvancementService.ResolveAdvancement(sf1, [sf1, final_]);

        modified.Should().HaveCount(1);
        final_.HomeTeamId.Should().Be("eagles");
        final_.HomeTeamPlaceholder.Should().Be("Winner SF1");
    }

    [Fact]
    public void ResolveAdvancement_WinnerPlaceholder_ResolvesAwayTeam()
    {
        var sf2 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "wolves", awayTeamId: "bears", label: "SF2");
        sf2.RecordSimpleResult(1, 2);

        var final_ = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2",
            label: "Final");

        var modified = BracketAdvancementService.ResolveAdvancement(sf2, [sf2, final_]);

        modified.Should().HaveCount(1);
        final_.AwayTeamId.Should().Be("bears");
        final_.AwayTeamPlaceholder.Should().Be("Winner SF2");
    }

    [Fact]
    public void ResolveAdvancement_LoserPlaceholder_ResolvesTeam()
    {
        var sf1 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks", label: "SF1");
        sf1.RecordSimpleResult(2, 1);

        var thirdPlace = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Loser SF1",
            awayTeamPlaceholder: "Loser SF2");

        var modified = BracketAdvancementService.ResolveAdvancement(sf1, [sf1, thirdPlace]);

        modified.Should().HaveCount(1);
        thirdPlace.HomeTeamId.Should().Be("hawks");
    }

    [Fact]
    public void ResolveAdvancement_Draw_DoesNotAdvance()
    {
        var sf1 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks", label: "SF1");
        sf1.RecordSimpleResult(1, 1);

        var final_ = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2");

        var modified = BracketAdvancementService.ResolveAdvancement(sf1, [sf1, final_]);

        modified.Should().BeEmpty();
        final_.HomeTeamId.Should().BeNull();
    }

    [Fact]
    public void ResolveAdvancement_NoLabel_ReturnsEmpty()
    {
        var rrGame = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks");
        rrGame.RecordSimpleResult(2, 1);

        var modified = BracketAdvancementService.ResolveAdvancement(rrGame, [rrGame]);

        modified.Should().BeEmpty();
    }

    [Fact]
    public void ResolveAdvancement_ResultCorrection_ReResolvesTeam()
    {
        var sf1 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks", label: "SF1");
        sf1.RecordSimpleResult(2, 1);

        var final_ = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2");

        BracketAdvancementService.ResolveAdvancement(sf1, [sf1, final_]);
        final_.HomeTeamId.Should().Be("eagles");

        sf1.RecordSimpleResult(1, 2);
        var modified = BracketAdvancementService.ResolveAdvancement(sf1, [sf1, final_]);

        modified.Should().HaveCount(1);
        final_.HomeTeamId.Should().Be("hawks");
    }

    [Fact]
    public void ResolveAdvancement_MultipleDownstreamGames_ResolvesAll()
    {
        var sf1 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks", label: "SF1");
        sf1.RecordSimpleResult(2, 1);

        var final_ = Game.Create("t1", "p1", "g1", 3,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2");
        var thirdPlace = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Loser SF1",
            awayTeamPlaceholder: "Loser SF2");

        var modified = BracketAdvancementService.ResolveAdvancement(
            sf1, [sf1, final_, thirdPlace]);

        modified.Should().HaveCount(2);
        final_.HomeTeamId.Should().Be("eagles");
        thirdPlace.HomeTeamId.Should().Be("hawks");
    }

    [Fact]
    public void ResolveAdvancement_NoMatchingPlaceholders_ReturnsEmpty()
    {
        var sf1 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks", label: "SF1");
        sf1.RecordSimpleResult(2, 1);

        var unrelatedGame = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Winner SF2",
            awayTeamPlaceholder: "Winner SF3");

        var modified = BracketAdvancementService.ResolveAdvancement(
            sf1, [sf1, unrelatedGame]);

        modified.Should().BeEmpty();
    }

    [Fact]
    public void ResolveAdvancement_CompletedDownstreamGame_UpdatesTeamIdsPreservesResult()
    {
        var sf1 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks", label: "SF1");
        sf1.RecordSimpleResult(2, 1);

        var final_ = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2");
        final_.HomeTeamId = "eagles";
        final_.AwayTeamId = "wolves";
        final_.RecordSimpleResult(2, 0);

        sf1.RecordSimpleResult(1, 2);
        var modified = BracketAdvancementService.ResolveAdvancement(sf1, [sf1, final_]);

        modified.Should().HaveCount(1);
        final_.HomeTeamId.Should().Be("hawks");
        final_.Status.Should().Be(GameStatus.Completed);
        final_.HomeScore.Should().Be(2);
    }

    [Fact]
    public void ResolveAdvancement_DoesNotModifySelf()
    {
        var sf1 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks", label: "SF1");
        sf1.RecordSimpleResult(2, 1);

        var modified = BracketAdvancementService.ResolveAdvancement(sf1, [sf1]);

        modified.Should().BeEmpty();
    }
}
