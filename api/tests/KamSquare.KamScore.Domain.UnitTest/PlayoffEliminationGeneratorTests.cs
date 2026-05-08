using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.Services.Formats;

namespace KamSquare.KamScore.Domain.UnitTest;

public class PlayoffEliminationGeneratorTests
{
    private const string TournamentId = "t1";
    private const string PhaseId = "p1";
    private const string GroupId = "g1";

    private readonly IPhaseFormatStrategy _strategy = new PlayoffEliminationStrategy();

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
    public void Generate_With2Teams_ShouldReturn1Game()
    {
        var teams = new List<string> { "a", "b" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().HaveCount(1);
        games[0].HomeTeamId.Should().Be("a");
        games[0].AwayTeamId.Should().Be("b");
        games[0].Round.Should().Be(1);
        games[0].HomeTeamPlaceholder.Should().BeNull();
        games[0].AwayTeamPlaceholder.Should().BeNull();
    }

    [Fact]
    public void Generate_With4Teams_ShouldReturn3Games()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().HaveCount(3);

        // 2 semifinal games (round 1) with real team IDs
        var semiFinals = games.Where(g => g.Round == 1).ToList();
        semiFinals.Should().HaveCount(2);
        semiFinals.Should().AllSatisfy(g =>
        {
            g.HomeTeamId.Should().NotBeNull();
            g.AwayTeamId.Should().NotBeNull();
        });

        // 1 final game (round 2) with placeholders
        var final_ = games.Where(g => g.Round == 2).ToList();
        final_.Should().HaveCount(1);
        var finalGame = final_[0];
        (finalGame.HomeTeamPlaceholder ?? finalGame.HomeTeamId).Should().NotBeNull();
        (finalGame.AwayTeamPlaceholder ?? finalGame.AwayTeamId).Should().NotBeNull();
    }

    [Fact]
    public void Generate_With4Teams_SeededCorrectly()
    {
        // Seed 1 vs Seed 4, Seed 2 vs Seed 3
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var round1 = games.Where(g => g.Round == 1).ToList();
        var allTeamsInRound1 = round1
            .SelectMany(g => new[] { g.HomeTeamId, g.AwayTeamId })
            .Where(t => t is not null)
            .ToList();

        // All 4 teams should appear in round 1
        allTeamsInRound1.Should().Contain("s1");
        allTeamsInRound1.Should().Contain("s2");
        allTeamsInRound1.Should().Contain("s3");
        allTeamsInRound1.Should().Contain("s4");

        // Seed 1 and Seed 4 should be in the same game
        var seed1Game = round1.First(g => g.HomeTeamId == "s1" || g.AwayTeamId == "s1");
        (seed1Game.HomeTeamId == "s4" || seed1Game.AwayTeamId == "s4").Should().BeTrue();
    }

    [Fact]
    public void Generate_With8Teams_ShouldReturn7Games()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().HaveCount(7); // 4 QF + 2 SF + 1 F

        var qf = games.Where(g => g.Round == 1).ToList();
        var sf = games.Where(g => g.Round == 2).ToList();
        var final_ = games.Where(g => g.Round == 3).ToList();

        qf.Should().HaveCount(4);
        sf.Should().HaveCount(2);
        final_.Should().HaveCount(1);

        // QF should have real team IDs
        qf.Should().AllSatisfy(g =>
        {
            g.HomeTeamId.Should().NotBeNull();
            g.AwayTeamId.Should().NotBeNull();
        });

        // SF and Final should have placeholders
        sf.Should().AllSatisfy(g =>
        {
            (g.HomeTeamPlaceholder is not null || g.HomeTeamId is not null).Should().BeTrue();
            (g.AwayTeamPlaceholder is not null || g.AwayTeamId is not null).Should().BeTrue();
        });
    }

    [Fact]
    public void Generate_With3Teams_ShouldReturn2Games_Seed1GetsBye()
    {
        var teams = new List<string> { "s1", "s2", "s3" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().HaveCount(2);

        // Round 1: one game between lower seeds
        var round1 = games.Where(g => g.Round == 1).ToList();
        round1.Should().HaveCount(1);
        round1[0].HomeTeamId.Should().NotBeNull();
        round1[0].AwayTeamId.Should().NotBeNull();
        // Seed 1 should NOT be in round 1 (has a bye)
        (round1[0].HomeTeamId == "s1" || round1[0].AwayTeamId == "s1").Should().BeFalse();

        // Round 2: final includes seed 1 directly
        var round2 = games.Where(g => g.Round == 2).ToList();
        round2.Should().HaveCount(1);
        (round2[0].HomeTeamId == "s1" || round2[0].AwayTeamId == "s1").Should().BeTrue();
    }

    [Fact]
    public void Generate_SetsCorrectIds()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, ["a", "b", "c", "d"]);

        games.Should().AllSatisfy(g =>
        {
            g.TournamentId.Should().Be(TournamentId);
            g.PhaseId.Should().Be(PhaseId);
            g.GroupId.Should().Be(GroupId);
        });
    }

    [Fact]
    public void NextPowerOfTwo_CalculatesCorrectly()
    {
        BracketUtilities.NextPowerOfTwo(1).Should().Be(1);
        BracketUtilities.NextPowerOfTwo(2).Should().Be(2);
        BracketUtilities.NextPowerOfTwo(3).Should().Be(4);
        BracketUtilities.NextPowerOfTwo(5).Should().Be(8);
        BracketUtilities.NextPowerOfTwo(8).Should().Be(8);
    }

    [Fact]
    public void BuildBracketOrder_With4_ShouldHaveCorrectSeeding()
    {
        var order = BracketUtilities.BuildBracketOrder(4);
        // Expected: [0, 3, 1, 2] so games are seed1vs4, seed2vs3
        order.Should().Equal(0, 3, 1, 2);
    }

    [Fact]
    public void BuildBracketOrder_With8_ShouldHaveCorrectSeeding()
    {
        var order = BracketUtilities.BuildBracketOrder(8);
        // Top half: seed1vs8, seed4vs5. Bottom half: seed2vs7, seed3vs6
        order.Should().Equal(0, 7, 3, 4, 1, 6, 2, 5);
    }

    [Fact]
    public void Generate_With2Teams_ShouldHaveFinalLabel()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, ["a", "b"]);

        games[0].Label.Should().Be("Final");
    }

    [Fact]
    public void Generate_With4Teams_SetsLabelsOnGames()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var semiFinals = games.Where(g => g.Round == 1).OrderBy(g => g.Label).ToList();
        semiFinals.Select(g => g.Label).Should().BeEquivalentTo(["SF1", "SF2"]);

        var final_ = games.Single(g => g.Round == 2);
        final_.Label.Should().Be("Final");
    }

    [Fact]
    public void Generate_With8Teams_SetsLabelsOnGames()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var qf = games.Where(g => g.Round == 1).ToList();
        qf.Select(g => g.Label).Should().BeEquivalentTo(["QF1", "QF2", "QF3", "QF4"]);

        var sf = games.Where(g => g.Round == 2).ToList();
        sf.Select(g => g.Label).Should().BeEquivalentTo(["SF1", "SF2"]);

        var final_ = games.Single(g => g.Round == 3);
        final_.Label.Should().Be("Final");
    }

    [Fact]
    public void Generate_LabelsMatchPlaceholderReferences()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        AssertLabelsMatchPlaceholderReferences(games);
    }

    [Fact]
    public void Generate_With5Teams_FinalReferencesSfWinners_NotRealTeam()
    {
        // Regression for Bug A: with 5 teams the Final must reference SF winners
        // via placeholders only — no real team id should leak into the Final.
        var teams = new List<string> { "s1", "s2", "s3", "s4", "s5" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var final_ = games.Single(g => g.Label == "Final");

        final_.HomeTeamId.Should().BeNull();
        final_.AwayTeamId.Should().BeNull();
        final_.HomeTeamPlaceholder.Should().Be("Winner SF1");
        final_.AwayTeamPlaceholder.Should().Be("Winner SF2");
    }

    [Fact]
    public void Generate_With5Teams_BracketStructure()
    {
        // 5 teams → bracketSize 8, 1 real R1 game, 3 byes. Natural seeded
        // layout (the GameScheduler handles rest-aware ordering by playing
        // fully-resolved games first):
        //   QF1 = s4 vs s5 (only real R1 game)
        //   SF1 = s1 (bye) vs Winner QF1 — top half of the bracket
        //   SF2 = s2 vs s3 — bottom half, both teams known
        var teams = new List<string> { "s1", "s2", "s3", "s4", "s5" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var round1 = games.Where(g => g.Round == 1).ToList();
        round1.Should().HaveCount(1);
        round1[0].Label.Should().Be("QF1");
        new HashSet<string?> { round1[0].HomeTeamId, round1[0].AwayTeamId }
            .Should().BeEquivalentTo(new HashSet<string?> { "s4", "s5" });

        var round2 = games.Where(g => g.Round == 2).ToList();
        round2.Should().HaveCount(2);

        var sf1 = round2.Single(g => g.Label == "SF1");
        var sf1RealIds = new[] { sf1.HomeTeamId, sf1.AwayTeamId }.Where(t => t is not null).ToList();
        var sf1Placeholders = new[] { sf1.HomeTeamPlaceholder, sf1.AwayTeamPlaceholder }
            .Where(p => p is not null).ToList();
        sf1RealIds.Should().BeEquivalentTo(["s1"]);
        sf1Placeholders.Should().BeEquivalentTo(["Winner QF1"]);

        var sf2 = round2.Single(g => g.Label == "SF2");
        new HashSet<string?> { sf2.HomeTeamId, sf2.AwayTeamId }
            .Should().BeEquivalentTo(new HashSet<string?> { "s2", "s3" });
        sf2.HomeTeamPlaceholder.Should().BeNull();
        sf2.AwayTeamPlaceholder.Should().BeNull();
    }

    [Fact]
    public void Generate_With7Teams_BracketStructure()
    {
        // 7 teams → bracketSize 8, 3 real R1 games (QF1..QF3), 1 bye (s1).
        // The bye-last reorder only fires when there is exactly ONE real R1
        // game; 7 teams has three, so the natural seeded layout applies:
        //   SF1 = s1 (bye) vs Winner QF1
        //   SF2 = Winner QF2 vs Winner QF3
        var teams = Enumerable.Range(1, 7).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var round1 = games.Where(g => g.Round == 1).ToList();
        round1.Should().HaveCount(3);
        round1.Select(g => g.Label).Should().BeEquivalentTo(["QF1", "QF2", "QF3"]);

        var round2 = games.Where(g => g.Round == 2).ToList();
        round2.Should().HaveCount(2);

        var sf1 = round2.Single(g => g.Label == "SF1");
        var sf1RealIds = new[] { sf1.HomeTeamId, sf1.AwayTeamId }.Where(t => t is not null).ToList();
        var sf1Placeholders = new[] { sf1.HomeTeamPlaceholder, sf1.AwayTeamPlaceholder }
            .Where(p => p is not null).ToList();
        sf1RealIds.Should().BeEquivalentTo(["s1"]);
        sf1Placeholders.Should().BeEquivalentTo(["Winner QF1"]);

        var sf2 = round2.Single(g => g.Label == "SF2");
        sf2.HomeTeamId.Should().BeNull();
        sf2.AwayTeamId.Should().BeNull();
        sf2.HomeTeamPlaceholder.Should().Be("Winner QF2");
        sf2.AwayTeamPlaceholder.Should().Be("Winner QF3");

        var final_ = games.Single(g => g.Round == 3);
        final_.Label.Should().Be("Final");
        final_.HomeTeamPlaceholder.Should().Be("Winner SF1");
        final_.AwayTeamPlaceholder.Should().Be("Winner SF2");
    }

    [Fact]
    public void Generate_With9Teams_BracketStructure()
    {
        // 9 teams → bracketSize 16, 1 real R1 game (s8 vs s9), 7 byes.
        // Natural seeded layout: QF1 contains s1 (top-of-bracket bye seed)
        // paired with the R1 winner; QF2..QF4 are all-known-team matchups
        // for s4/s5, s2/s7, s3/s6. The first-round game uses bracket-size
        // naming with a hyphen separator ("R16-1"), avoiding the "R11"
        // visual confusion the legacy ordinal naming produced.
        var teams = Enumerable.Range(1, 9).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var round1 = games.Where(g => g.Round == 1).ToList();
        round1.Should().HaveCount(1);
        round1[0].Label.Should().Be("R16-1");
        new HashSet<string?> { round1[0].HomeTeamId, round1[0].AwayTeamId }
            .Should().BeEquivalentTo(new HashSet<string?> { "s8", "s9" });

        var qfs = games.Where(g => g.Round == 2).ToList();
        qfs.Should().HaveCount(4);
        qfs.Select(g => g.Label).Should().BeEquivalentTo(["QF1", "QF2", "QF3", "QF4"]);

        // QF1 = s1 (bye) + "Winner R16-1".
        var qf1 = qfs.Single(g => g.Label == "QF1");
        var qf1RealIds = new[] { qf1.HomeTeamId, qf1.AwayTeamId }.Where(t => t is not null).ToList();
        var qf1Placeholders = new[] { qf1.HomeTeamPlaceholder, qf1.AwayTeamPlaceholder }
            .Where(p => p is not null).ToList();
        qf1RealIds.Should().BeEquivalentTo(["s1"]);
        qf1Placeholders.Should().BeEquivalentTo(["Winner R16-1"]);

        // QF2..QF4 are all-known-team matchups (s4/s5, s2/s7, s3/s6 — order-agnostic).
        var laterQfs = qfs.Where(g => g.Label is "QF2" or "QF3" or "QF4").ToList();
        laterQfs.Should().AllSatisfy(g =>
        {
            g.HomeTeamId.Should().NotBeNull();
            g.AwayTeamId.Should().NotBeNull();
            g.HomeTeamPlaceholder.Should().BeNull();
            g.AwayTeamPlaceholder.Should().BeNull();
        });
        var laterMatchups = laterQfs
            .Select(g => new HashSet<string?> { g.HomeTeamId, g.AwayTeamId })
            .ToList();
        laterMatchups.Should().ContainEquivalentOf(new HashSet<string?> { "s4", "s5" });
        laterMatchups.Should().ContainEquivalentOf(new HashSet<string?> { "s2", "s7" });
        laterMatchups.Should().ContainEquivalentOf(new HashSet<string?> { "s3", "s6" });
    }

    private static void AssertLabelsMatchPlaceholderReferences(List<Game> games)
    {
        var allLabels = games.Where(g => g.Label is not null).Select(g => g.Label!).ToHashSet();

        foreach (var game in games)
        {
            if (game.HomeTeamPlaceholder is not null)
            {
                var referencedLabel = game.HomeTeamPlaceholder
                    .Replace("Winner ", "").Replace("Loser ", "");
                allLabels.Should().Contain(referencedLabel,
                    $"HomeTeamPlaceholder '{game.HomeTeamPlaceholder}' references non-existent label");
            }
            if (game.AwayTeamPlaceholder is not null)
            {
                var referencedLabel = game.AwayTeamPlaceholder
                    .Replace("Winner ", "").Replace("Loser ", "");
                allLabels.Should().Contain(referencedLabel,
                    $"AwayTeamPlaceholder '{game.AwayTeamPlaceholder}' references non-existent label");
            }
        }
    }
}
