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

        var modified = BracketUtilities.ResolveAdvancement(sf1, [sf1, final_]);

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

        var modified = BracketUtilities.ResolveAdvancement(sf2, [sf2, final_]);

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

        var modified = BracketUtilities.ResolveAdvancement(sf1, [sf1, thirdPlace]);

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

        var modified = BracketUtilities.ResolveAdvancement(sf1, [sf1, final_]);

        modified.Should().BeEmpty();
        final_.HomeTeamId.Should().BeNull();
    }

    [Fact]
    public void ResolveAdvancement_NoLabel_ReturnsEmpty()
    {
        var rrGame = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks");
        rrGame.RecordSimpleResult(2, 1);

        var modified = BracketUtilities.ResolveAdvancement(rrGame, [rrGame]);

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

        BracketUtilities.ResolveAdvancement(sf1, [sf1, final_]);
        final_.HomeTeamId.Should().Be("eagles");

        sf1.RecordSimpleResult(1, 2);
        var modified = BracketUtilities.ResolveAdvancement(sf1, [sf1, final_]);

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

        var modified = BracketUtilities.ResolveAdvancement(
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

        var modified = BracketUtilities.ResolveAdvancement(
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
        var modified = BracketUtilities.ResolveAdvancement(sf1, [sf1, final_]);

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

        var modified = BracketUtilities.ResolveAdvancement(sf1, [sf1]);

        modified.Should().BeEmpty();
    }

    // --- Referee Placeholder Resolution ---

    [Fact]
    public void ResolveAdvancement_LoserRefereePlaceholder_ResolvesToRealTeam()
    {
        var qf1 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks", label: "QF1");
        qf1.RecordSimpleResult(2, 1); // eagles win, hawks lose

        var sf1 = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Winner QF1",
            awayTeamPlaceholder: "Winner QF2",
            label: "SF1");
        sf1.RefereeTeamPlaceholder = "Loser QF1";

        var modified = BracketUtilities.ResolveAdvancement(qf1, [qf1, sf1]);

        sf1.RefereeTeamId.Should().Be("hawks");
        sf1.RefereeTeamPlaceholder.Should().Be("Loser QF1", "placeholder label should be preserved");
        modified.Should().Contain(sf1);
    }

    [Fact]
    public void ResolveAdvancement_WinnerRefereePlaceholder_ResolvesToRealTeam()
    {
        var qf3 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "wolves", awayTeamId: "bears", label: "QF3");
        qf3.RecordSimpleResult(2, 0); // wolves win

        var sf1 = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Winner QF1",
            awayTeamPlaceholder: "Winner QF2",
            label: "SF1");
        sf1.RefereeTeamPlaceholder = "Winner QF3";

        var modified = BracketUtilities.ResolveAdvancement(qf3, [qf3, sf1]);

        sf1.RefereeTeamId.Should().Be("wolves");
        sf1.RefereeTeamPlaceholder.Should().Be("Winner QF3");
        modified.Should().Contain(sf1);
    }

    [Fact]
    public void ResolveAdvancement_NoRefereePlaceholder_DoesNotSetRefereeTeamId()
    {
        var sf1 = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "eagles", awayTeamId: "hawks", label: "SF1");
        sf1.RecordSimpleResult(2, 1);

        var final_ = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2",
            label: "Final");
        // No RefereeTeamPlaceholder set

        BracketUtilities.ResolveAdvancement(sf1, [sf1, final_]);

        final_.RefereeTeamId.Should().BeNull();
    }
}
