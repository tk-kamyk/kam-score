using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Services.Formats;

namespace KamSquare.KamScore.Domain.UnitTest;

public class DoubleEliminationVdGeneratorTests
{
    private const string TournamentId = "t1";
    private const string PhaseId = "p1";
    private const string GroupId = "g1";

    private readonly IPhaseFormatStrategy _strategy = new DoubleEliminationVdStrategy();

    private static List<string> EightTeams() =>
        Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();

    [Fact]
    public void Generate_With8Teams_ShouldGenerate14Games()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());
        games.Should().HaveCount(14);
    }

    [Fact]
    public void Generate_WithNon8Teams_FallsBackToStandardDoubleElimination()
    {
        // For team counts other than 8, the VD strategy must silently fall back
        // to the standard Double Elimination format and produce identical games.
        var standard = new DoubleEliminationStrategy();

        AssertVdMatchesStandardDoubleElimination(
            standard,
            ["s1", "s2", "s3", "s4"]);

        AssertVdMatchesStandardDoubleElimination(
            standard,
            Enumerable.Range(1, 7).Select(i => $"s{i}").ToList());

        AssertVdMatchesStandardDoubleElimination(
            standard,
            Enumerable.Range(1, 9).Select(i => $"s{i}").ToList());
    }

    private void AssertVdMatchesStandardDoubleElimination(
        DoubleEliminationStrategy standard, List<string> teamIds)
    {
        var vdGames = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teamIds);
        var standardGames = standard.GenerateGames(TournamentId, PhaseId, GroupId, teamIds);

        var vdTuples = vdGames
            .Select(g => (g.Round, g.Label, g.HomeTeamId, g.AwayTeamId,
                g.HomeTeamPlaceholder, g.AwayTeamPlaceholder))
            .OrderBy(t => t.Round).ThenBy(t => t.Label, StringComparer.Ordinal)
            .ToList();
        var standardTuples = standardGames
            .Select(g => (g.Round, g.Label, g.HomeTeamId, g.AwayTeamId,
                g.HomeTeamPlaceholder, g.AwayTeamPlaceholder))
            .OrderBy(t => t.Round).ThenBy(t => t.Label, StringComparer.Ordinal)
            .ToList();

        vdTuples.Should().Equal(standardTuples,
            $"VD strategy with {teamIds.Count} teams should mirror standard Double Elimination");
    }

    [Fact]
    public void Generate_Round1_Has4QfGamesWithRealTeams()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        var qfGames = games.Where(g => g.Round == 1).ToList();
        qfGames.Should().HaveCount(4);
        qfGames.Should().AllSatisfy(g =>
        {
            g.HomeTeamId.Should().NotBeNull();
            g.AwayTeamId.Should().NotBeNull();
            g.Label.Should().StartWith("QF");
        });
    }

    [Fact]
    public void Generate_QfSeeding_IsCorrect()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        var qfGames = games.Where(g => g.Round == 1).OrderBy(g => g.Label).ToList();

        // QF1: seed1 vs seed8
        qfGames[0].Label.Should().Be("QF1");
        qfGames[0].HomeTeamId.Should().Be("s1");
        qfGames[0].AwayTeamId.Should().Be("s8");

        // QF2: seed4 vs seed5
        qfGames[1].Label.Should().Be("QF2");
        qfGames[1].HomeTeamId.Should().Be("s4");
        qfGames[1].AwayTeamId.Should().Be("s5");

        // QF3: seed2 vs seed7
        qfGames[2].Label.Should().Be("QF3");
        qfGames[2].HomeTeamId.Should().Be("s2");
        qfGames[2].AwayTeamId.Should().Be("s7");

        // QF4: seed3 vs seed6
        qfGames[3].Label.Should().Be("QF4");
        qfGames[3].HomeTeamId.Should().Be("s3");
        qfGames[3].AwayTeamId.Should().Be("s6");
    }

    [Fact]
    public void Generate_Round2_Has2WinnersGames()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        var wGames = games.Where(g => g.Round == 2).ToList();
        wGames.Should().HaveCount(2);

        var w1 = wGames.Single(g => g.Label == "W1");
        w1.HomeTeamPlaceholder.Should().Be("Winner QF1");
        w1.AwayTeamPlaceholder.Should().Be("Winner QF2");

        var w2 = wGames.Single(g => g.Label == "W2");
        w2.HomeTeamPlaceholder.Should().Be("Winner QF3");
        w2.AwayTeamPlaceholder.Should().Be("Winner QF4");
    }

    [Fact]
    public void Generate_Round3_Has2LosersGames()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        var lGames = games.Where(g => g.Round == 3).ToList();
        lGames.Should().HaveCount(2);

        var l1 = lGames.Single(g => g.Label == "L1");
        l1.HomeTeamPlaceholder.Should().Be("Loser QF1");
        l1.AwayTeamPlaceholder.Should().Be("Loser QF2");

        var l2 = lGames.Single(g => g.Label == "L2");
        l2.HomeTeamPlaceholder.Should().Be("Loser QF3");
        l2.AwayTeamPlaceholder.Should().Be("Loser QF4");
    }

    [Fact]
    public void Generate_Round4_Has2CrossoverGames_CrossBracket()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        var xGames = games.Where(g => g.Round == 4).ToList();
        xGames.Should().HaveCount(2);

        // X1: Loser W2 vs Winner L1 (cross-bracket)
        var x1 = xGames.Single(g => g.Label == "X1");
        x1.HomeTeamPlaceholder.Should().Be("Loser W2");
        x1.AwayTeamPlaceholder.Should().Be("Winner L1");

        // X2: Loser W1 vs Winner L2 (cross-bracket)
        var x2 = xGames.Single(g => g.Label == "X2");
        x2.HomeTeamPlaceholder.Should().Be("Loser W1");
        x2.AwayTeamPlaceholder.Should().Be("Winner L2");
    }

    [Fact]
    public void Generate_Round5_Has2GrandSemiFinalsGames()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        var gsfGames = games.Where(g => g.Round == 5).ToList();
        gsfGames.Should().HaveCount(2);

        // GSF1: Winner W1 vs Winner X1
        var gsf1 = gsfGames.Single(g => g.Label == "GSF1");
        gsf1.HomeTeamPlaceholder.Should().Be("Winner W1");
        gsf1.AwayTeamPlaceholder.Should().Be("Winner X1");

        // GSF2: Winner W2 vs Winner X2
        var gsf2 = gsfGames.Single(g => g.Label == "GSF2");
        gsf2.HomeTeamPlaceholder.Should().Be("Winner W2");
        gsf2.AwayTeamPlaceholder.Should().Be("Winner X2");
    }

    [Fact]
    public void Generate_Round6_Has7thPlaceGame()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        var seventhGames = games.Where(g => g.Round == 6).ToList();
        seventhGames.Should().HaveCount(1);

        var seventh = seventhGames[0];
        seventh.Label.Should().Be("7th Place");
        seventh.HomeTeamPlaceholder.Should().Be("Loser L1");
        seventh.AwayTeamPlaceholder.Should().Be("Loser L2");
    }

    [Fact]
    public void Generate_Round7_HasGrandFinal()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        var gfGames = games.Where(g => g.Round == 7).ToList();
        gfGames.Should().HaveCount(1);

        var gf = gfGames[0];
        gf.Label.Should().Be("Grand Final");
        gf.HomeTeamPlaceholder.Should().Be("Winner GSF1");
        gf.AwayTeamPlaceholder.Should().Be("Winner GSF2");
    }

    [Fact]
    public void Generate_AllGamesHaveUniqueLabels()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        var labels = games.Select(g => g.Label!).ToList();
        labels.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Generate_EveryGameHasALabel()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());
        games.Should().AllSatisfy(g => g.Label.Should().NotBeNull());
    }

    [Fact]
    public void Generate_AllPlaceholdersReferenceExistingLabels()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        var allLabels = games.Select(g => g.Label!).ToHashSet();

        foreach (var game in games)
        {
            AssertPlaceholderReferences(game.HomeTeamPlaceholder, allLabels, "HomeTeamPlaceholder");
            AssertPlaceholderReferences(game.AwayTeamPlaceholder, allLabels, "AwayTeamPlaceholder");
        }
    }

    [Fact]
    public void Generate_SetsCorrectIds()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        games.Should().AllSatisfy(g =>
        {
            g.TournamentId.Should().Be(TournamentId);
            g.PhaseId.Should().Be(PhaseId);
            g.GroupId.Should().Be(GroupId);
        });
    }

    [Fact]
    public void Generate_RoundsAreSequential()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, EightTeams());

        var rounds = games.Select(g => g.Round).ToList();
        for (var i = 1; i < rounds.Count; i++)
        {
            rounds[i].Should().BeGreaterThanOrEqualTo(rounds[i - 1]);
        }
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
