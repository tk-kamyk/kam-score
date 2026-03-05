using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class ResultApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public ResultApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeGameRepository);
        Fake.Reset(factory.FakeStructureRepository);
    }

    private Tournament CreateTestTournament(string ownerId = "alice")
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, ownerId);
        tournament.Update("Summer Cup", Discipline.Volleyball, new DateTime(2026, 6, 1), 30, null);
        return tournament;
    }

    private Game CreateScheduledGame(string tournamentId)
    {
        var game = Game.Create(tournamentId, "p1", "g1", 1,
            homeTeamId: "team1", awayTeamId: "team2", refereeTeamId: "team3");
        game.AssignSchedule("court1", new DateTime(2026, 6, 1, 9, 0, 0));
        return game;
    }

    private TournamentStructure CreateInProgressStructure(string tournamentId, string phaseId = "p1")
    {
        var structure = TournamentStructure.Create(tournamentId);
        var phase = structure.AddPhase("Phase 1", PhaseFormat.RoundRobin, 1);
        // Override the auto-generated id to match the game's phaseId
        phase.Id = phaseId;
        phase.Activate();
        return structure;
    }

    private void SetupTournamentAndGame(Tournament tournament, Game game)
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeGameRepository.GetByIdAsync(tournament.Id, game.Id)).Returns(game);
        A.CallTo(() => _factory.FakeGameRepository.UpdateAsync(A<Game>.Ignored))
            .ReturnsLazily((Game g) => Task.FromResult(g));

        var structure = CreateInProgressStructure(tournament.Id, game.PhaseId);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);
    }

    private static GameResultDto DetailedResult() => new(
        Sets: [new SetResultDto(25, 20), new SetResultDto(23, 25), new SetResultDto(15, 10)],
        HomeScore: null,
        AwayScore: null);

    private static GameResultDto SimpleResult() => new(
        Sets: null,
        HomeScore: 2,
        AwayScore: 1);

    [Fact]
    public async Task RecordResult_Owner_ReturnsOk()
    {
        var tournament = CreateTestTournament("alice");
        var game = CreateScheduledGame(tournament.Id);
        SetupTournamentAndGame(tournament, game);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{game.Id}/result",
            DetailedResult());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GameDto>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Completed");
        result.HomeScore.Should().Be(2);
        result.AwayScore.Should().Be(1);
    }

    [Fact]
    public async Task RecordResult_WithValidTournamentCode_ReturnsOk()
    {
        var tournament = CreateTestTournament("alice");
        var game = CreateScheduledGame(tournament.Id);
        SetupTournamentAndGame(tournament, game);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tournament-Code", tournament.TournamentCode);

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{game.Id}/result",
            DetailedResult());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GameDto>();
        result!.Status.Should().Be("Completed");
        result.Sets.Should().HaveCount(3);
    }

    [Fact]
    public async Task RecordResult_WithValidCodeCaseInsensitive_ReturnsOk()
    {
        var tournament = CreateTestTournament("alice");
        var game = CreateScheduledGame(tournament.Id);
        SetupTournamentAndGame(tournament, game);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tournament-Code", tournament.TournamentCode.ToLower());

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{game.Id}/result",
            SimpleResult());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RecordResult_SimpleScore_WithValidCode_ReturnsOk()
    {
        var tournament = CreateTestTournament("alice");
        var game = CreateScheduledGame(tournament.Id);
        SetupTournamentAndGame(tournament, game);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tournament-Code", tournament.TournamentCode);

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{game.Id}/result",
            SimpleResult());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GameDto>();
        result!.HomeScore.Should().Be(2);
        result.AwayScore.Should().Be(1);
        result.Sets.Should().BeNullOrEmpty();
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task RecordResult_InvalidCode_Returns403()
    {
        var tournament = CreateTestTournament("alice");
        var game = CreateScheduledGame(tournament.Id);
        SetupTournamentAndGame(tournament, game);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tournament-Code", "XXXX");

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{game.Id}/result",
            DetailedResult());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RecordResult_NoAuth_Returns401()
    {
        var tournament = CreateTestTournament("alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);

        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/some-game/result",
            DetailedResult());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecordResult_NonOwnerJwt_Returns403()
    {
        var tournament = CreateTestTournament("alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);

        var client = _factory.CreateAuthenticatedClient("bob");

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/some-game/result",
            DetailedResult());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RecordResult_GameNotFound_Returns404()
    {
        var tournament = CreateTestTournament("alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeGameRepository.GetByIdAsync(tournament.Id, "missing-game"))
            .Returns((Game?)null);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/missing-game/result",
            DetailedResult());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RecordResult_BothSetsAndScores_Returns400()
    {
        var tournament = CreateTestTournament("alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);

        var client = _factory.CreateAuthenticatedClient("alice");
        var bothResult = new GameResultDto(
            Sets: [new SetResultDto(25, 20), new SetResultDto(25, 18)],
            HomeScore: 2,
            AwayScore: 0);

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/some-game/result",
            bothResult);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecordResult_InvalidBody_NeitherSetsNorScore_Returns400()
    {
        var tournament = CreateTestTournament("alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);

        var client = _factory.CreateAuthenticatedClient("alice");
        var invalidResult = new GameResultDto(Sets: null, HomeScore: null, AwayScore: null);

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/some-game/result",
            invalidResult);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecordResult_SimpleResult_Tie_Returns400()
    {
        var tournament = CreateTestTournament("alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);

        var client = _factory.CreateAuthenticatedClient("alice");
        var tieResult = new GameResultDto(Sets: null, HomeScore: 1, AwayScore: 1);

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/some-game/result",
            tieResult);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecordResult_DetailedResult_TwoSets_Tie_Returns400()
    {
        var tournament = CreateTestTournament("alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);

        var client = _factory.CreateAuthenticatedClient("alice");
        var tieResult = new GameResultDto(
            Sets: [new SetResultDto(25, 20), new SetResultDto(20, 25)],
            HomeScore: null,
            AwayScore: null);

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/some-game/result",
            tieResult);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecordResult_DetailedResult_OneSet_Tie_ReturnsOk()
    {
        var tournament = CreateTestTournament("alice");
        var game = CreateScheduledGame(tournament.Id);
        SetupTournamentAndGame(tournament, game);

        var client = _factory.CreateAuthenticatedClient("alice");
        var tieResult = new GameResultDto(
            Sets: [new SetResultDto(25, 25)],
            HomeScore: null,
            AwayScore: null);

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{game.Id}/result",
            tieResult);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GameDto>();
        result!.Status.Should().Be("Completed");
        result.HomeScore.Should().Be(0);
        result.AwayScore.Should().Be(0);
    }

    [Fact]
    public async Task RecordResult_DetailedResult_MultiSet_IndividualSetDraw_Returns400()
    {
        var tournament = CreateTestTournament("alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);

        var client = _factory.CreateAuthenticatedClient("alice");
        var drawResult = new GameResultDto(
            Sets: [new SetResultDto(25, 13), new SetResultDto(13, 13)],
            HomeScore: null,
            AwayScore: null);

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/some-game/result",
            drawResult);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecordResult_AlreadyCompleted_Owner_ReturnsOk()
    {
        var tournament = CreateTestTournament("alice");
        var game = CreateScheduledGame(tournament.Id);
        game.RecordSimpleResult(2, 1);
        SetupTournamentAndGame(tournament, game);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{game.Id}/result",
            SimpleResult());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RecordResult_UpdatesGameInRepository()
    {
        var tournament = CreateTestTournament("alice");
        var game = CreateScheduledGame(tournament.Id);
        SetupTournamentAndGame(tournament, game);

        var client = _factory.CreateAuthenticatedClient("alice");

        await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{game.Id}/result",
            SimpleResult());

        A.CallTo(() => _factory.FakeGameRepository.UpdateAsync(A<Game>.That.Matches(
            g => g.Status == GameStatus.Completed &&
                 g.HomeScore == 2 &&
                 g.AwayScore == 1)))
            .MustHaveHappenedOnceExactly();
    }

    // --- Bracket Advancement ---

    [Fact]
    public async Task RecordResult_PlayoffGame_AdvancesWinnerToDownstreamGame()
    {
        var tournament = CreateTestTournament("alice");

        var sf1 = Game.Create(tournament.Id, "p1", "g1", 1,
            homeTeamId: "team1", awayTeamId: "team2", label: "SF1");
        sf1.AssignSchedule("court1", new DateTime(2026, 6, 1, 9, 0, 0));

        var final_ = Game.Create(tournament.Id, "p1", "g1", 2,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2",
            label: "Final");

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeGameRepository.GetByIdAsync(tournament.Id, sf1.Id)).Returns(sf1);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, "p1"))
            .Returns(new List<Game> { sf1, final_ });
        A.CallTo(() => _factory.FakeGameRepository.UpdateAsync(A<Game>.Ignored))
            .ReturnsLazily((Game g) => Task.FromResult(g));
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(CreateInProgressStructure(tournament.Id));

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{sf1.Id}/result",
            SimpleResult());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        A.CallTo(() => _factory.FakeGameRepository.UpdateAsync(A<Game>.That.Matches(
            g => g.Id == final_.Id && g.HomeTeamId == "team1")))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RecordResult_PlayoffGame_AdvancesLoserToPlacementGame()
    {
        var tournament = CreateTestTournament("alice");

        var sf1 = Game.Create(tournament.Id, "p1", "g1", 1,
            homeTeamId: "team1", awayTeamId: "team2", label: "SF1");
        sf1.AssignSchedule("court1", new DateTime(2026, 6, 1, 9, 0, 0));

        var thirdPlace = Game.Create(tournament.Id, "p1", "g1", 2,
            homeTeamPlaceholder: "Loser SF1",
            awayTeamPlaceholder: "Loser SF2");

        var final_ = Game.Create(tournament.Id, "p1", "g1", 3,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2",
            label: "Final");

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeGameRepository.GetByIdAsync(tournament.Id, sf1.Id)).Returns(sf1);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, "p1"))
            .Returns(new List<Game> { sf1, thirdPlace, final_ });
        A.CallTo(() => _factory.FakeGameRepository.UpdateAsync(A<Game>.Ignored))
            .ReturnsLazily((Game g) => Task.FromResult(g));
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(CreateInProgressStructure(tournament.Id));

        var client = _factory.CreateAuthenticatedClient("alice");

        await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{sf1.Id}/result",
            SimpleResult());

        // Winner (team1) → Final home, Loser (team2) → 3rd place home
        A.CallTo(() => _factory.FakeGameRepository.UpdateAsync(A<Game>.That.Matches(
            g => g.Id == thirdPlace.Id && g.HomeTeamId == "team2")))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _factory.FakeGameRepository.UpdateAsync(A<Game>.That.Matches(
            g => g.Id == final_.Id && g.HomeTeamId == "team1")))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RecordResult_RoundRobinGame_DoesNotFetchSiblingGames()
    {
        var tournament = CreateTestTournament("alice");
        var game = CreateScheduledGame(tournament.Id);
        SetupTournamentAndGame(tournament, game);

        var client = _factory.CreateAuthenticatedClient("alice");

        await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{game.Id}/result",
            SimpleResult());

        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(
            A<string>.Ignored, A<string>.Ignored))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task RecordResult_PlayoffDraw_DoesNotAdvance()
    {
        var tournament = CreateTestTournament("alice");

        var sf1 = Game.Create(tournament.Id, "p1", "g1", 1,
            homeTeamId: "team1", awayTeamId: "team2", label: "SF1");
        sf1.AssignSchedule("court1", new DateTime(2026, 6, 1, 9, 0, 0));

        var final_ = Game.Create(tournament.Id, "p1", "g1", 2,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2");

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeGameRepository.GetByIdAsync(tournament.Id, sf1.Id)).Returns(sf1);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, "p1"))
            .Returns(new List<Game> { sf1, final_ });
        A.CallTo(() => _factory.FakeGameRepository.UpdateAsync(A<Game>.Ignored))
            .ReturnsLazily((Game g) => Task.FromResult(g));
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(CreateInProgressStructure(tournament.Id));

        var client = _factory.CreateAuthenticatedClient("alice");

        var drawResult = new GameResultDto(
            Sets: [new SetResultDto(25, 25)],
            HomeScore: null, AwayScore: null);

        await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{sf1.Id}/result",
            drawResult);

        // Only the completed game should be updated, not the final
        A.CallTo(() => _factory.FakeGameRepository.UpdateAsync(A<Game>.That.Matches(
            g => g.Id == final_.Id)))
            .MustNotHaveHappened();
    }
}
