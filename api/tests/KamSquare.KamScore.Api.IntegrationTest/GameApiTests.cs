using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class GameApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public GameApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeStructureRepository);
        Fake.Reset(factory.FakeTeamRepository);
        Fake.Reset(factory.FakeCourtRepository);
        Fake.Reset(factory.FakeGameRepository);
    }

    private Tournament CreateTestTournament(string ownerId = "alice")
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, ownerId);
        tournament.Update("Summer Cup", Discipline.Volleyball,
            new DateTime(2026, 6, 1), 30, null);
        return tournament;
    }

    private TournamentStructure CreateRoundRobinStructure(string tournamentId, int teamCount = 3)
    {
        var structure = TournamentStructure.Create(tournamentId);
        var phase = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 1,
            startTime: new TimeOnly(9, 0));
        var group = phase.Groups[0];
        for (var i = 0; i < teamCount; i++)
            group.TeamIds.Add($"team{i + 1}");
        return structure;
    }

    private TournamentStructure CreatePlayoffStructure(string tournamentId, PhaseFormat format, int teamCount = 4)
    {
        var structure = TournamentStructure.Create(tournamentId);
        var phase = structure.AddPhase("Playoffs", format, 1,
            startTime: new TimeOnly(10, 0));
        var group = phase.Groups[0];
        for (var i = 0; i < teamCount; i++)
            group.TeamIds.Add($"team{i + 1}");
        return structure;
    }

    private List<Court> CreateCourts(string tournamentId, int count = 2)
    {
        var courts = new List<Court>();
        for (var i = 0; i < count; i++)
            courts.Add(Court.Create($"Court {i + 1}", tournamentId));
        return courts;
    }

    private List<Team> CreateTeams(string tournamentId, int count = 3)
    {
        var teams = new List<Team>();
        for (var i = 0; i < count; i++)
        {
            var team = Team.Create($"Team {i + 1}", 50 - i * 10, tournamentId);
            // Override the auto-generated ID to match structure's team references
            typeof(Entity).GetProperty("Id")!.SetValue(team, $"team{i + 1}");
            teams.Add(team);
        }
        return teams;
    }

    private void SetupFakes(Tournament tournament, TournamentStructure structure,
        List<Court> courts, List<Team> teams)
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(courts);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(teams);
        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(
            A<string>.Ignored, A<string>.Ignored)).Returns(false);
        A.CallTo(() => _factory.FakeGameRepository.CreateBatchAsync(A<IEnumerable<Game>>.Ignored))
            .ReturnsLazily((IEnumerable<Game> games) => Task.FromResult(games));
    }

    [Fact]
    public async Task GenerateAndSchedule_RoundRobin_ShouldReturnCreated()
    {
        var tournament = CreateTestTournament();
        var structure = CreateRoundRobinStructure(tournament.Id, 3);
        var courts = CreateCourts(tournament.Id);
        var teams = CreateTeams(tournament.Id, 3);
        SetupFakes(tournament, structure, courts, teams);

        var client = _factory.CreateAuthenticatedClient("alice");
        var phaseId = structure.Phases[0].Id;

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phaseId}/generate-schedule", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var games = await response.Content.ReadFromJsonAsync<List<GameDto>>();
        games.Should().NotBeNull();
        games!.Should().HaveCount(3); // 3 teams = 3 games
        games.Should().AllSatisfy(g =>
        {
            g.CourtId.Should().NotBeNull();
            g.StartTime.Should().NotBeNull();
            g.Status.Should().Be("Scheduled");
        });

        A.CallTo(() => _factory.FakeGameRepository.CreateBatchAsync(A<IEnumerable<Game>>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GenerateAndSchedule_RoundRobin_ShouldResolveTeamNames()
    {
        var tournament = CreateTestTournament();
        var structure = CreateRoundRobinStructure(tournament.Id, 3);
        var courts = CreateCourts(tournament.Id);
        var teams = CreateTeams(tournament.Id, 3);
        SetupFakes(tournament, structure, courts, teams);

        var client = _factory.CreateAuthenticatedClient("alice");
        var phaseId = structure.Phases[0].Id;

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phaseId}/generate-schedule", null);

        var games = await response.Content.ReadFromJsonAsync<List<GameDto>>();
        games!.Should().AllSatisfy(g =>
        {
            g.HomeTeamName.Should().NotBeNull();
            g.AwayTeamName.Should().NotBeNull();
            g.CourtName.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task GenerateAndSchedule_PlayoffElimination_ShouldReturnGamesWithPlaceholders()
    {
        var tournament = CreateTestTournament();
        var structure = CreatePlayoffStructure(tournament.Id, PhaseFormat.PlayoffElimination, 4);
        var courts = CreateCourts(tournament.Id);
        var teams = CreateTeams(tournament.Id, 4);
        SetupFakes(tournament, structure, courts, teams);

        var client = _factory.CreateAuthenticatedClient("alice");
        var phaseId = structure.Phases[0].Id;

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phaseId}/generate-schedule", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var games = await response.Content.ReadFromJsonAsync<List<GameDto>>();
        games.Should().NotBeNull();
        games!.Count.Should().Be(3); // 2 SF + 1 F

        // Final should have placeholders
        var finalGame = games.FirstOrDefault(g => g.Round == 2);
        finalGame.Should().NotBeNull();
        (finalGame!.HomeTeamPlaceholder is not null || finalGame.HomeTeamId is not null).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAndSchedule_NotOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        var structure = CreateRoundRobinStructure(tournament.Id);
        var courts = CreateCourts(tournament.Id);
        var teams = CreateTeams(tournament.Id);
        SetupFakes(tournament, structure, courts, teams);

        var client = _factory.CreateAuthenticatedClient("bob");
        var phaseId = structure.Phases[0].Id;

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phaseId}/generate-schedule", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateAndSchedule_Anonymous_ShouldReturn401()
    {
        var tournament = CreateTestTournament();
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/fake-phase/generate-schedule", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateAndSchedule_GamesAlreadyExist_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = CreateRoundRobinStructure(tournament.Id);
        var courts = CreateCourts(tournament.Id);
        var teams = CreateTeams(tournament.Id);
        SetupFakes(tournament, structure, courts, teams);

        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(
            tournament.Id, structure.Phases[0].Id)).Returns(true);

        var client = _factory.CreateAuthenticatedClient("alice");
        var phaseId = structure.Phases[0].Id;

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phaseId}/generate-schedule", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateAndSchedule_NoCourts_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = CreateRoundRobinStructure(tournament.Id);
        var teams = CreateTeams(tournament.Id);

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id)).Returns(structure);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new List<Court>());
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id)).Returns(teams);

        var client = _factory.CreateAuthenticatedClient("alice");
        var phaseId = structure.Phases[0].Id;

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phaseId}/generate-schedule", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateAndSchedule_NoGameLength_ShouldReturn400()
    {
        var tournament = Tournament.Create("Cup", Discipline.Volleyball, "alice");
        // GameLength is null by default
        var structure = CreateRoundRobinStructure(tournament.Id);
        var courts = CreateCourts(tournament.Id);
        var teams = CreateTeams(tournament.Id);

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id)).Returns(structure);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(tournament.Id)).Returns(courts);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id)).Returns(teams);

        var client = _factory.CreateAuthenticatedClient("alice");
        var phaseId = structure.Phases[0].Id;

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phaseId}/generate-schedule", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetGames_ShouldReturnGamesWithNames()
    {
        var tournament = CreateTestTournament();
        var courts = CreateCourts(tournament.Id);
        var teams = CreateTeams(tournament.Id, 3);

        var testGames = new List<Game>
        {
            Game.Create(tournament.Id, "p1", "g1", 1,
                homeTeamId: "team1", awayTeamId: "team2", refereeTeamId: "team3")
        };
        testGames[0].AssignSchedule(courts[0].Id, new DateTime(2026, 6, 1, 9, 0, 0));

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeGameRepository.GetGamesAsync(tournament.Id, null, null, null)).Returns(testGames);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id)).Returns(teams);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(tournament.Id)).Returns(courts);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/games");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var games = await response.Content.ReadFromJsonAsync<List<GameDto>>();
        games.Should().HaveCount(1);
        games![0].HomeTeamName.Should().Be("Team 1");
        games[0].AwayTeamName.Should().Be("Team 2");
        games[0].RefereeTeamName.Should().Be("Team 3");
        games[0].CourtName.Should().Be("Court 1");
    }

    [Fact]
    public async Task GetGames_WithPhaseFilter_ShouldFilterByPhase()
    {
        var tournament = CreateTestTournament();
        var courts = CreateCourts(tournament.Id);
        var teams = CreateTeams(tournament.Id);

        var testGames = new List<Game>
        {
            Game.Create(tournament.Id, "p1", "g1", 1, homeTeamId: "team1", awayTeamId: "team2"),
            Game.Create(tournament.Id, "p2", "g2", 1, homeTeamId: "team1", awayTeamId: "team3")
        };

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeGameRepository.GetGamesAsync(tournament.Id, "p1", null, null))
            .Returns(new List<Game> { testGames[0] });
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id)).Returns(teams);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(tournament.Id)).Returns(courts);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/games?phaseId=p1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var games = await response.Content.ReadFromJsonAsync<List<GameDto>>();
        games.Should().HaveCount(1);
        games![0].PhaseId.Should().Be("p1");
    }

    [Fact]
    public async Task GetGames_Anonymous_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeGameRepository.GetGamesAsync(tournament.Id, null, null, null))
            .Returns(new List<Game>());
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new List<Team>());
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new List<Court>());

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/games");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteGames_Owner_ShouldReturnNoContent()
    {
        var tournament = CreateTestTournament();
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);

        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Phase 1", PhaseFormat.RoundRobin, 1);
        phase.Id = "phase1";
        phase.Activate();
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id)).Returns(structure);
        A.CallTo(() => _factory.FakeStructureRepository.UpdateAsync(A<TournamentStructure>.Ignored))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));

        var client = _factory.CreateAuthenticatedClient("alice");
        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/phase1/games");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        A.CallTo(() => _factory.FakeGameRepository.DeleteByPhaseIdAsync(tournament.Id, "phase1"))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteGames_NotOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);

        var client = _factory.CreateAuthenticatedClient("bob");
        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/phase1/games");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
