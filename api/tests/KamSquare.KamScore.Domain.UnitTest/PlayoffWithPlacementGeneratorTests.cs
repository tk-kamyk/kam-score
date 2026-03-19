using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Services.Formats;

namespace KamSquare.KamScore.Domain.UnitTest;

public class PlayoffWithPlacementGeneratorTests
{
    private const string TournamentId = "t1";
    private const string PhaseId = "p1";
    private const string GroupId = "g1";

    private readonly IPhaseFormatStrategy _strategy = new PlayoffWithPlacementStrategy();

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
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, ["a", "b"]);
        games.Should().HaveCount(1);
        games[0].HomeTeamId.Should().Be("a");
        games[0].AwayTeamId.Should().Be("b");
        games[0].Round.Should().Be(1);
    }

    [Fact]
    public void Generate_With3Teams_ShouldReturn2Games()
    {
        // 3 teams: bracketSize=4, 1 bye
        // R1: 1 real game (seeds 2 vs 3), seed 1 gets bye
        // Final: seed 1 vs winner of R1
        // Only 1 R1 loser → no 3rd place game possible
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId,
            ["s1", "s2", "s3"]);

        games.Should().HaveCount(2);
        games.Where(g => g.Round == 1).Should().HaveCount(1, "1 real R1 game");
    }

    [Fact]
    public void Generate_With4Teams_ShouldReturn4Games()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        // 2 SF + 1 Third-place + 1 Final = 4 games
        games.Should().HaveCount(4);

        // Round 1: SF1, SF2
        games.Where(g => g.Round == 1).Should().HaveCount(2);
        // Round 2: 3rd place
        games.Where(g => g.Round == 2).Should().HaveCount(1);
        // Round 3: Final
        games.Where(g => g.Round == 3).Should().HaveCount(1);
    }

    [Fact]
    public void Generate_With4Teams_ThirdPlaceMatchHasCorrectPlaceholders()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        // 3rd place is round 2 (placement: losers before winners)
        var thirdPlace = games.Single(g => g.Round == 2);
        thirdPlace.HomeTeamPlaceholder.Should().Contain("Loser SF1");
        thirdPlace.AwayTeamPlaceholder.Should().Contain("Loser SF2");
    }

    [Fact]
    public void Generate_With4Teams_FinalHasCorrectPlaceholders()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var final_ = games.Single(g => g.Round == 3);
        final_.HomeTeamPlaceholder.Should().Contain("Winner SF1");
        final_.AwayTeamPlaceholder.Should().Contain("Winner SF2");
    }

    [Fact]
    public void Generate_With4Teams_FinalIsLastRound()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var maxRound = games.Max(g => g.Round);
        var finalGame = games.Single(g => g.Round == maxRound);
        finalGame.HomeTeamPlaceholder.Should().Contain("Winner");
        finalGame.AwayTeamPlaceholder.Should().Contain("Winner");
    }

    [Fact]
    public void Generate_With8Teams_ShouldReturn12Games()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        // 4 QF + 2 B-SF + 2 A-SF + 7th + 5th + 3rd + Final = 12
        games.Should().HaveCount(12);
    }

    [Fact]
    public void Generate_With8Teams_CorrectRoundOrdering()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        // Round 1: QF (4 games)
        games.Where(g => g.Round == 1).Should().HaveCount(4);

        // Round 2: B-SF (2 games from QF losers)
        var bSf = games.Where(g => g.Round == 2).ToList();
        bSf.Should().HaveCount(2);
        bSf.Should().AllSatisfy(g =>
        {
            g.HomeTeamPlaceholder.Should().Contain("Loser QF");
            g.AwayTeamPlaceholder.Should().Contain("Loser QF");
        });

        // Round 3: A-SF (2 games from QF winners)
        var aSf = games.Where(g => g.Round == 3).ToList();
        aSf.Should().HaveCount(2);
        aSf.Should().AllSatisfy(g =>
        {
            g.HomeTeamPlaceholder.Should().Contain("Winner QF");
            g.AwayTeamPlaceholder.Should().Contain("Winner QF");
        });

        // Round 4: 7th place (B-SF losers)
        var seventh = games.Single(g => g.Round == 4);
        seventh.HomeTeamPlaceholder.Should().Contain("Loser B-SF");
        seventh.AwayTeamPlaceholder.Should().Contain("Loser B-SF");

        // Round 5: 5th place (B-SF winners)
        var fifth = games.Single(g => g.Round == 5);
        fifth.HomeTeamPlaceholder.Should().Contain("Winner B-SF");
        fifth.AwayTeamPlaceholder.Should().Contain("Winner B-SF");

        // Round 6: 3rd place (A-SF losers)
        var third = games.Single(g => g.Round == 6);
        third.HomeTeamPlaceholder.Should().Contain("Loser A-SF");
        third.AwayTeamPlaceholder.Should().Contain("Loser A-SF");

        // Round 7: Final (A-SF winners)
        var final_ = games.Single(g => g.Round == 7);
        final_.HomeTeamPlaceholder.Should().Contain("Winner A-SF");
        final_.AwayTeamPlaceholder.Should().Contain("Winner A-SF");
    }

    [Fact]
    public void Generate_With8Teams_FinalIsLastRound()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        var maxRound = games.Max(g => g.Round);
        maxRound.Should().Be(7);

        var finalGame = games.Single(g => g.Round == maxRound);
        finalGame.HomeTeamPlaceholder.Should().Contain("Winner");
        finalGame.AwayTeamPlaceholder.Should().Contain("Winner");
    }

    [Fact]
    public void Generate_SetsCorrectIds()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId,
            ["a", "b", "c", "d"]);

        games.Should().AllSatisfy(g =>
        {
            g.TournamentId.Should().Be(TournamentId);
            g.PhaseId.Should().Be(PhaseId);
            g.GroupId.Should().Be(GroupId);
        });
    }

    [Fact]
    public void Generate_PlacementGamesHavePlaceholders()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        // Rounds 2 and 3 are placement games (3rd place and Final)
        var placementGames = games.Where(g => g.Round > 1).ToList();
        placementGames.Should().HaveCount(2);
        placementGames.Should().AllSatisfy(g =>
        {
            (g.HomeTeamPlaceholder ?? g.HomeTeamId).Should().NotBeNull();
            (g.AwayTeamPlaceholder ?? g.AwayTeamId).Should().NotBeNull();
        });
    }

    [Fact]
    public void Generate_With2Teams_ShouldHaveFinalLabel()
    {
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, ["a", "b"]);

        games[0].Label.Should().Be("Final");
    }

    [Fact]
    public void Generate_With4Teams_AllGamesHaveLabels()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().AllSatisfy(g => g.Label.Should().NotBeNullOrEmpty());

        var semiFinals = games.Where(g => g.Round == 1).ToList();
        semiFinals.Select(g => g.Label).Should().BeEquivalentTo(["SF1", "SF2"]);
    }

    [Fact]
    public void Generate_With8Teams_AllGamesHaveLabels()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        games.Should().AllSatisfy(g => g.Label.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void Generate_LabelsMatchPlaceholderReferences()
    {
        var teams = new List<string> { "s1", "s2", "s3", "s4" };
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        AssertLabelsMatchPlaceholderReferences(games);
    }

    [Fact]
    public void Generate_With8Teams_LabelsMatchPlaceholderReferences()
    {
        var teams = Enumerable.Range(1, 8).Select(i => $"s{i}").ToList();
        var games = _strategy.GenerateGames(TournamentId, PhaseId, GroupId, teams);

        AssertLabelsMatchPlaceholderReferences(games);
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
