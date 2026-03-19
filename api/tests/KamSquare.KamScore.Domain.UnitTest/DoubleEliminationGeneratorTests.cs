using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Services.Formats;

namespace KamSquare.KamScore.Domain.UnitTest;

public class DoubleEliminationGeneratorTests
{
    private const string TournamentId = "t1";
    private const string PhaseId = "p1";
    private const string GroupId = "g1";

    private readonly IPhaseFormatStrategy _strategy = new DoubleEliminationStrategy();

    [Fact]
    public void Generate_WithEmptyTeams_ShouldReturnEmpty()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, []);
        games.Should().BeEmpty();
    }

    [Fact]
    public void Generate_With1Team_ShouldReturnEmpty()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, ["a"]);
        games.Should().BeEmpty();
    }

    [Fact]
    public void Generate_With2Teams_ShouldReturnGrandFinalOnly()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, ["a", "b"]);

        games.Should().HaveCount(1);
        games[0].HomeTeamId.Should().Be("a");
        games[0].AwayTeamId.Should().Be("b");
        games[0].Label.Should().Be("Grand Final");
    }

    [Fact]
    public void Generate_With4Teams_ShouldGenerateCorrectGameCount()
    {
        // 4 teams: WB (2 SF + 1 WB-F) + LB (1 LB-R1 + 1 LB-R2) + GF = 6 games
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().HaveCount(6);
    }

    [Fact]
    public void Generate_With4Teams_ShouldHaveWbGames()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var wbGames = games.Where(g => g.Label is not null && g.Label.StartsWith("WB-")).ToList();
        wbGames.Should().HaveCount(3); // 2 SF + 1 WB-Final
    }

    [Fact]
    public void Generate_With4Teams_ShouldHaveLbGames()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var lbGames = games.Where(g => g.Label is not null && g.Label.StartsWith("LB-")).ToList();
        lbGames.Should().HaveCount(2); // LB-R1 (SF losers) + LB-R2 (LB-R1 winner vs WB-F loser)
    }

    [Fact]
    public void Generate_With4Teams_ShouldHaveGrandFinal()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var gf = games.Where(g => g.Label == "Grand Final").ToList();
        gf.Should().HaveCount(1);
    }

    [Fact]
    public void Generate_With4Teams_WbFirstRoundHasRealTeams()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var wbRound1 = games.Where(g => g.Round == 1).ToList();
        wbRound1.Should().HaveCount(2);
        wbRound1.Should().AllSatisfy(g =>
        {
            g.HomeTeamId.Should().NotBeNull();
            g.AwayTeamId.Should().NotBeNull();
        });
    }

    [Fact]
    public void Generate_With4Teams_SeedingCorrect()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var wbRound1 = games.Where(g => g.Round == 1).ToList();
        var allTeamsR1 = wbRound1
            .SelectMany(g => new[] { g.HomeTeamId, g.AwayTeamId })
            .Where(t => t is not null)
            .ToList();

        allTeamsR1.Should().Contain("s1");
        allTeamsR1.Should().Contain("s2");
        allTeamsR1.Should().Contain("s3");
        allTeamsR1.Should().Contain("s4");

        // Seed 1 vs Seed 4 in same game
        var seed1Game = wbRound1.First(g => g.HomeTeamId == "s1" || g.AwayTeamId == "s1");
        (seed1Game.HomeTeamId == "s4" || seed1Game.AwayTeamId == "s4").Should().BeTrue();
    }

    [Fact]
    public void Generate_With8Teams_ShouldGenerateCorrectGameCount()
    {
        // 8 teams: WB (4 QF + 2 SF + 1 WB-F) + LB (2 LB-R1 + 2 LB-R2 + 1 LB-R3 + 1 LB-R4) + GF = 14
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        // WB: 7 games, LB: 6 games, GF: 1 = 14 total
        games.Should().HaveCount(14);
    }

    [Fact]
    public void Generate_With8Teams_WbHas7Games()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var wbGames = games.Where(g => g.Label is not null && g.Label.StartsWith("WB-")).ToList();
        wbGames.Should().HaveCount(7); // 4 QF + 2 SF + 1 WB-F
    }

    [Fact]
    public void Generate_With8Teams_LbHas6Games()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var lbGames = games.Where(g => g.Label is not null && g.Label.StartsWith("LB-")).ToList();
        lbGames.Should().HaveCount(6); // R1: 2, R2: 2, R3: 1, R4: 1
    }

    [Fact]
    public void Generate_With8Teams_GrandFinalIsLastRound()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var gf = games.Single(g => g.Label == "Grand Final");
        gf.Round.Should().Be(games.Max(g => g.Round));
    }

    [Fact]
    public void Generate_With8Teams_RoundsAreInOrder()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        // Rounds should be sequential and non-decreasing in game order
        var rounds = games.Select(g => g.Round).ToList();
        for (var i = 1; i < rounds.Count; i++)
        {
            rounds[i].Should().BeGreaterThanOrEqualTo(rounds[i - 1]);
        }
    }

    [Fact]
    public void Generate_SetsCorrectIds()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().AllSatisfy(g =>
        {
            g.TournamentId.Should().Be(TournamentId);
            g.PhaseId.Should().Be(PhaseId);
            g.GroupId.Should().Be(GroupId);
        });
    }

    [Fact]
    public void Generate_AllPlaceholdersReferenceExistingLabels()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var allLabels = games.Where(g => g.Label is not null).Select(g => g.Label!).ToHashSet();

        foreach (var game in games)
        {
            AssertPlaceholderReferences(game.HomeTeamPlaceholder, allLabels, "HomeTeamPlaceholder");
            AssertPlaceholderReferences(game.AwayTeamPlaceholder, allLabels, "AwayTeamPlaceholder");
        }
    }

    [Fact]
    public void Generate_With4Teams_AllPlaceholdersReferenceExistingLabels()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var allLabels = games.Where(g => g.Label is not null).Select(g => g.Label!).ToHashSet();

        foreach (var game in games)
        {
            AssertPlaceholderReferences(game.HomeTeamPlaceholder, allLabels, "HomeTeamPlaceholder");
            AssertPlaceholderReferences(game.AwayTeamPlaceholder, allLabels, "AwayTeamPlaceholder");
        }
    }

    [Fact]
    public void Generate_With3Teams_Seed1GetsByeInWb()
    {
        var teams = new List<string> { "s1", "s2", "s3" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        // WB R1 should have 1 game (s2 vs s3 or s3 vs s2), seed 1 gets a bye
        var wbR1 = games.Where(g => g.Round == 1).ToList();
        wbR1.Should().HaveCount(1);
        (wbR1[0].HomeTeamId == "s1" || wbR1[0].AwayTeamId == "s1").Should().BeFalse();
    }

    [Fact]
    public void Generate_GrandFinalReferencesWbWinnerAndLbWinner()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var gf = games.Single(g => g.Label == "Grand Final");

        // Home should reference WB winner
        (gf.HomeTeamPlaceholder is not null || gf.HomeTeamId is not null).Should().BeTrue();
        if (gf.HomeTeamPlaceholder is not null)
            gf.HomeTeamPlaceholder.Should().StartWith("Winner WB-");

        // Away should reference LB winner
        (gf.AwayTeamPlaceholder is not null || gf.AwayTeamId is not null).Should().BeTrue();
        if (gf.AwayTeamPlaceholder is not null)
            gf.AwayTeamPlaceholder.Should().Contain("LB-");
    }

    [Fact]
    public void Generate_LbGamesUsePlaceholdersForWbLosers()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var lbR1 = games.Where(g => g.Label is not null && g.Label.StartsWith("LB-R1")).ToList();
        lbR1.Should().HaveCount(1);

        // LB R1 should reference losers from WB
        var game = lbR1[0];
        var hasLoserRef = (game.HomeTeamPlaceholder?.StartsWith("Loser WB-") ?? false)
            || (game.AwayTeamPlaceholder?.StartsWith("Loser WB-") ?? false);
        hasLoserRef.Should().BeTrue();
    }

    [Fact]
    public void Generate_With8Teams_InterleavesWbAndLbRounds()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        // Expected interleaved order for 8 teams:
        // R1: WB-QF (4 games)
        // R2: WB-SF (2 games)
        // R3: LB-R1 (2 games, QF losers play each other)
        // R4: LB-R2 (2 games, LB-R1 winners vs SF losers)
        // R5: LB-R3 (1 game, LB elimination before WB-Final)
        // R6: WB-Final (1 game) — interleaved after LB rounds
        // R7: LB-R4 (1 game, vs WB-Final loser)
        // R8: Grand Final

        var wbQfGames = games.Where(g => g.Label!.StartsWith("WB-QF")).ToList();
        wbQfGames.Should().HaveCount(4);
        wbQfGames.Should().AllSatisfy(g => g.Round.Should().Be(1));

        var wbSfGames = games.Where(g => g.Label!.StartsWith("WB-SF")).ToList();
        wbSfGames.Should().HaveCount(2);
        wbSfGames.Should().AllSatisfy(g => g.Round.Should().Be(2));

        var lbR1Games = games.Where(g => g.Label!.StartsWith("LB-R1")).ToList();
        lbR1Games.Should().HaveCount(2);
        lbR1Games.Should().AllSatisfy(g => g.Round.Should().Be(3));

        var lbR2Games = games.Where(g => g.Label!.StartsWith("LB-R2")).ToList();
        lbR2Games.Should().HaveCount(2);
        lbR2Games.Should().AllSatisfy(g => g.Round.Should().Be(4));

        var lbR3Games = games.Where(g => g.Label!.StartsWith("LB-R3")).ToList();
        lbR3Games.Should().HaveCount(1);
        lbR3Games[0].Round.Should().Be(5);

        var wbFinal = games.Single(g => g.Label == "WB-Final");
        wbFinal.Round.Should().Be(6, "WB-Final should be after LB-R1/R2/R3 for interleaving");

        var lbR4Games = games.Where(g => g.Label!.StartsWith("LB-R4")).ToList();
        lbR4Games.Should().HaveCount(1);
        lbR4Games[0].Round.Should().Be(7);

        var gf = games.Single(g => g.Label == "Grand Final");
        gf.Round.Should().Be(8);
    }

    [Fact]
    public void Generate_With4Teams_WbFinishesBeforeLb()
    {
        // 4 teams: not enough rounds to interleave, WB finishes first
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        // R1: WB-SF (2 games), R2: WB-Final (1 game), R3: LB-R1 (1 game), R4: LB-R2 (1 game), R5: GF
        var wbFinal = games.Single(g => g.Label == "WB-Final");
        wbFinal.Round.Should().Be(2);

        var lbR1 = games.Where(g => g.Label!.StartsWith("LB-R1")).ToList();
        lbR1.Should().AllSatisfy(g => g.Round.Should().Be(3));

        var lbR2 = games.Where(g => g.Label!.StartsWith("LB-R2")).ToList();
        lbR2.Should().AllSatisfy(g => g.Round.Should().Be(4));

        var gf = games.Single(g => g.Label == "Grand Final");
        gf.Round.Should().Be(5);
    }

    [Fact]
    public void Generate_AllGamesHaveUniqueLabels()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var labels = games.Where(g => g.Label is not null).Select(g => g.Label!).ToList();
        labels.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Generate_EveryGameHasALabel()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().AllSatisfy(g => g.Label.Should().NotBeNull());
    }

    private static void AssertPlaceholderReferences(string? placeholder, HashSet<string> allLabels, string field)
    {
        if (placeholder is null) return;

        var referencedLabel = placeholder
            .Replace("Winner ", "")
            .Replace("Loser ", "");
        allLabels.Should().Contain(referencedLabel,
            $"{field} '{placeholder}' references non-existent label");
    }
}
