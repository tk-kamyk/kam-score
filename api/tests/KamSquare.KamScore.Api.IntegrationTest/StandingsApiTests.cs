using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class StandingsApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public StandingsApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeStructureRepository);
        Fake.Reset(factory.FakeTeamRepository);
        Fake.Reset(factory.FakeGameRepository);
    }

    private Tournament CreateTestTournament(string ownerId = "alice")
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, ownerId);
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

    private List<Team> CreateTeams(string tournamentId, int count = 3)
    {
        var teams = new List<Team>();
        for (var i = 0; i < count; i++)
        {
            var team = Team.Create($"Team {i + 1}", 50 - i * 10, tournamentId);
            typeof(Entity).GetProperty("Id")!.SetValue(team, $"team{i + 1}");
            teams.Add(team);
        }
        return teams;
    }

    private static Game CreateCompletedGame(
        string tournamentId, string phaseId, string groupId,
        string homeTeamId, string awayTeamId, int homeScore, int awayScore, int round = 1)
    {
        var game = Game.Create(tournamentId, phaseId, groupId, round,
            homeTeamId: homeTeamId, awayTeamId: awayTeamId);
        game.RecordSimpleResult(homeScore, awayScore);
        return game;
    }

    private void SetupFakes(Tournament tournament, TournamentStructure structure, List<Team> teams,
        IEnumerable<Game>? games = null)
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(teams);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, A<string>.Ignored))
            .Returns(games ?? []);
    }

    [Fact]
    public async Task GetStandings_RoundRobin_WithCompletedGames_ReturnsCorrectOrdering()
    {
        var tournament = CreateTestTournament();
        var structure = CreateRoundRobinStructure(tournament.Id, 3);
        var teams = CreateTeams(tournament.Id, 3);
        var phaseId = structure.Phases[0].Id;
        var groupId = structure.Phases[0].Groups[0].Id;

        var games = new List<Game>
        {
            CreateCompletedGame(tournament.Id, phaseId, groupId, "team1", "team2", 2, 0),
            CreateCompletedGame(tournament.Id, phaseId, groupId, "team1", "team3", 2, 0),
            CreateCompletedGame(tournament.Id, phaseId, groupId, "team2", "team3", 2, 1),
        };

        SetupFakes(tournament, structure, teams, games);

        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/tournaments/{tournament.Id}/standings?phaseId={phaseId}&groupId={groupId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var standings = await response.Content.ReadFromJsonAsync<List<StandingDto>>();
        standings.Should().NotBeNull();
        standings!.Should().HaveCount(3);

        standings[0].TeamId.Should().Be("team1");
        standings[0].Points.Should().Be(4);
        standings[0].Position.Should().Be(1);

        standings[1].TeamId.Should().Be("team2");
        standings[1].Points.Should().Be(2);
        standings[1].Position.Should().Be(2);

        standings[2].TeamId.Should().Be("team3");
        standings[2].Points.Should().Be(0);
        standings[2].Position.Should().Be(3);
    }

    [Fact]
    public async Task GetStandings_RoundRobin_ReturnsTeamNames()
    {
        var tournament = CreateTestTournament();
        var structure = CreateRoundRobinStructure(tournament.Id, 2);
        var teams = CreateTeams(tournament.Id, 2);
        var phaseId = structure.Phases[0].Id;
        var groupId = structure.Phases[0].Groups[0].Id;

        var games = new List<Game>
        {
            CreateCompletedGame(tournament.Id, phaseId, groupId, "team1", "team2", 2, 0),
        };

        SetupFakes(tournament, structure, teams, games);

        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/tournaments/{tournament.Id}/standings?phaseId={phaseId}&groupId={groupId}");

        var standings = await response.Content.ReadFromJsonAsync<List<StandingDto>>();
        standings![0].TeamName.Should().Be("Team 1");
        standings[1].TeamName.Should().Be("Team 2");
    }

    [Fact]
    public async Task GetStandings_PlayoffElimination_ReturnsPositionsFromBracket()
    {
        var tournament = CreateTestTournament();
        var structure = CreatePlayoffStructure(tournament.Id, PhaseFormat.PlayoffElimination, 4);
        var teams = CreateTeams(tournament.Id, 4);
        var phaseId = structure.Phases[0].Id;
        var groupId = structure.Phases[0].Groups[0].Id;

        var games = new List<Game>
        {
            CreateCompletedGame(tournament.Id, phaseId, groupId, "team1", "team4", 2, 0, round: 1),
            CreateCompletedGame(tournament.Id, phaseId, groupId, "team2", "team3", 2, 0, round: 1),
            CreateCompletedGame(tournament.Id, phaseId, groupId, "team1", "team2", 2, 1, round: 2),
        };

        SetupFakes(tournament, structure, teams, games);

        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/tournaments/{tournament.Id}/standings?phaseId={phaseId}&groupId={groupId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var standings = await response.Content.ReadFromJsonAsync<List<StandingDto>>();

        standings!.First(s => s.TeamId == "team1").Position.Should().Be(1);
        standings.First(s => s.TeamId == "team2").Position.Should().Be(2);
        standings.First(s => s.TeamId == "team3").Position.Should().Be(3);
        standings.First(s => s.TeamId == "team4").Position.Should().Be(3);

        // Elimination should not have RR-specific fields
        standings.Should().AllSatisfy(s => s.Points.Should().BeNull());
    }

    [Fact]
    public async Task GetStandings_Anonymous_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = CreateRoundRobinStructure(tournament.Id, 2);
        var teams = CreateTeams(tournament.Id, 2);
        var phaseId = structure.Phases[0].Id;
        var groupId = structure.Phases[0].Groups[0].Id;

        SetupFakes(tournament, structure, teams);

        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/tournaments/{tournament.Id}/standings?phaseId={phaseId}&groupId={groupId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStandings_NonexistentTournament_Returns404()
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync("nonexistent"))
            .Returns((Tournament?)null);

        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            "/api/tournaments/nonexistent/standings?phaseId=p1&groupId=g1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStandings_NoGames_ReturnsAllTeamsAtZero()
    {
        var tournament = CreateTestTournament();
        var structure = CreateRoundRobinStructure(tournament.Id, 3);
        var teams = CreateTeams(tournament.Id, 3);
        var phaseId = structure.Phases[0].Id;
        var groupId = structure.Phases[0].Groups[0].Id;

        SetupFakes(tournament, structure, teams);

        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/tournaments/{tournament.Id}/standings?phaseId={phaseId}&groupId={groupId}");

        var standings = await response.Content.ReadFromJsonAsync<List<StandingDto>>();
        standings.Should().HaveCount(3);
        standings.Should().AllSatisfy(s =>
        {
            s.GamesPlayed.Should().Be(0);
            s.Points.Should().Be(0);
        });
    }

    [Fact]
    public async Task GetStandings_NonexistentPhase_Returns404()
    {
        var tournament = CreateTestTournament();
        var structure = CreateRoundRobinStructure(tournament.Id, 2);
        var teams = CreateTeams(tournament.Id, 2);
        SetupFakes(tournament, structure, teams);

        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/tournaments/{tournament.Id}/standings?phaseId=nonexistent&groupId=g1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStandings_NonexistentGroup_Returns404()
    {
        var tournament = CreateTestTournament();
        var structure = CreateRoundRobinStructure(tournament.Id, 2);
        var teams = CreateTeams(tournament.Id, 2);
        var phaseId = structure.Phases[0].Id;
        SetupFakes(tournament, structure, teams);

        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/tournaments/{tournament.Id}/standings?phaseId={phaseId}&groupId=nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
