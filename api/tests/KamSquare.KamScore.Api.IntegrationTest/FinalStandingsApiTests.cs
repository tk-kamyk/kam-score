using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class FinalStandingsApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public FinalStandingsApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeStructureRepository);
        Fake.Reset(factory.FakeTeamRepository);
        Fake.Reset(factory.FakeGameRepository);
    }

    private Tournament CreateTestTournament(string ownerId = "alice")
    {
        return Tournament.Create("Summer Cup", Discipline.Volleyball, ownerId);
    }

    private List<Team> CreateTeams(string tournamentId, params string[] names)
    {
        var teams = new List<Team>();
        for (var i = 0; i < names.Length; i++)
        {
            var team = Team.Create(names[i], 50 - i * 10, tournamentId);
            typeof(Entity).GetProperty("Id")!.SetValue(team, $"team{i + 1}");
            teams.Add(team);
        }
        return teams;
    }

    private static Game CompletedGame(string tournamentId, string phaseId, string groupId,
        string home, string away, int homeScore, int awayScore, int round = 1)
    {
        var game = Game.Create(tournamentId, phaseId, groupId, round,
            homeTeamId: home, awayTeamId: away);
        game.RecordSimpleResult(homeScore, awayScore);
        return game;
    }

    private void SetupFakes(Tournament tournament, TournamentStructure structure,
        List<Team> teams, List<Game> games)
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(teams);
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(games);
    }

    [Fact]
    public async Task GetFinalStandings_SinglePhase_ReturnsCorrectPositions()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 1);
        phase.Status = PhaseStatus.Completed;
        var groupId = phase.Groups[0].Id;
        phase.Groups[0].TeamIds.AddRange(["team1", "team2", "team3"]);

        var teams = CreateTeams(tournament.Id, "Eagles", "Hawks", "Wolves");

        var games = new List<Game>
        {
            CompletedGame(tournament.Id, phase.Id, groupId, "team1", "team2", 2, 0),
            CompletedGame(tournament.Id, phase.Id, groupId, "team1", "team3", 2, 0),
            CompletedGame(tournament.Id, phase.Id, groupId, "team2", "team3", 2, 1),
        };

        SetupFakes(tournament, structure, teams, games);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/final-standings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FinalStandingsResponseDto>();
        result.Should().NotBeNull();
        result!.Provisional.Should().BeFalse();
        result.Standings.Should().HaveCount(3);
        result.Standings[0].TeamName.Should().Be("Eagles");
        result.Standings[0].Position.Should().Be(1);
        result.Standings[1].TeamName.Should().Be("Hawks");
        result.Standings[2].TeamName.Should().Be("Wolves");
    }

    [Fact]
    public async Task GetFinalStandings_TwoPhases_EliminatedTeamsGetLowerPositions()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);

        var phase1 = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 1,
            groupWinners: 2);
        phase1.Status = PhaseStatus.Completed;
        var g1 = phase1.Groups[0];
        g1.TeamIds.AddRange(["team1", "team2", "team3", "team4"]);

        var phase2 = structure.AddPhase("Final", PhaseFormat.RoundRobin, 1);
        phase2.Status = PhaseStatus.Completed;
        var g2 = phase2.Groups[0];
        g2.TeamIds.AddRange(["team1", "team2"]);

        var teams = CreateTeams(tournament.Id, "Eagles", "Hawks", "Wolves", "Bears");

        var games = new List<Game>
        {
            CompletedGame(tournament.Id, phase1.Id, g1.Id, "team1", "team2", 2, 1),
            CompletedGame(tournament.Id, phase1.Id, g1.Id, "team1", "team3", 2, 0),
            CompletedGame(tournament.Id, phase1.Id, g1.Id, "team1", "team4", 2, 0),
            CompletedGame(tournament.Id, phase1.Id, g1.Id, "team2", "team3", 2, 1),
            CompletedGame(tournament.Id, phase1.Id, g1.Id, "team2", "team4", 2, 0),
            CompletedGame(tournament.Id, phase1.Id, g1.Id, "team3", "team4", 2, 1),
            CompletedGame(tournament.Id, phase2.Id, g2.Id, "team2", "team1", 2, 0),
        };

        SetupFakes(tournament, structure, teams, games);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/final-standings");

        var result = await response.Content.ReadFromJsonAsync<FinalStandingsResponseDto>();
        result!.Standings.Should().HaveCount(4);
        result.Standings[0].TeamName.Should().Be("Hawks");
        result.Standings[0].Position.Should().Be(1);
        result.Standings[1].TeamName.Should().Be("Eagles");
        result.Standings[1].Position.Should().Be(2);
        result.Standings[2].TeamName.Should().Be("Wolves");
        result.Standings[2].Position.Should().Be(3);
        result.Standings[3].TeamName.Should().Be("Bears");
        result.Standings[3].Position.Should().Be(4);
    }

    [Fact]
    public async Task GetFinalStandings_Provisional_WhenPhaseInProgress()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);

        var phase = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 1);
        phase.Status = PhaseStatus.InProgress;
        var groupId = phase.Groups[0].Id;
        phase.Groups[0].TeamIds.AddRange(["team1", "team2"]);

        var teams = CreateTeams(tournament.Id, "Eagles", "Hawks");

        var games = new List<Game>
        {
            CompletedGame(tournament.Id, phase.Id, groupId, "team1", "team2", 2, 0),
        };

        SetupFakes(tournament, structure, teams, games);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/final-standings");

        var result = await response.Content.ReadFromJsonAsync<FinalStandingsResponseDto>();
        result!.Provisional.Should().BeTrue();
    }

    [Fact]
    public async Task GetFinalStandings_NoGames_ReturnsEmptyStandings()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 1);

        var teams = CreateTeams(tournament.Id, "Eagles", "Hawks");

        SetupFakes(tournament, structure, teams, []);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/final-standings");

        var result = await response.Content.ReadFromJsonAsync<FinalStandingsResponseDto>();
        result!.Standings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFinalStandings_ExcludesPlaceholderTeams()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 1);
        phase.Status = PhaseStatus.Completed;
        var groupId = phase.Groups[0].Id;
        phase.Groups[0].TeamIds.AddRange(["team1", "team2"]);

        var teams = new List<Team>
        {
            CreateTeams(tournament.Id, "Eagles", "Hawks")[0],
            CreateTeams(tournament.Id, "Eagles", "Hawks")[1],
            Team.CreatePlaceholder("P1 - Seed 1", tournament.Id, phase.Id, 1),
        };
        typeof(Entity).GetProperty("Id")!.SetValue(teams[0], "team1");
        typeof(Entity).GetProperty("Id")!.SetValue(teams[1], "team2");

        var games = new List<Game>
        {
            CompletedGame(tournament.Id, phase.Id, groupId, "team1", "team2", 2, 0),
        };

        SetupFakes(tournament, structure, teams, games);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/final-standings");

        var result = await response.Content.ReadFromJsonAsync<FinalStandingsResponseDto>();
        result!.Standings.Should().HaveCount(2);
        result.Standings.Should().NotContain(s => s.TeamName.Contains("Seed"));
    }

    [Fact]
    public async Task GetFinalStandings_Anonymous_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 1);

        SetupFakes(tournament, structure, [], []);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/final-standings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFinalStandings_NonexistentTournament_Returns404()
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync("nonexistent"))
            .Returns((Tournament?)null);

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tournaments/nonexistent/final-standings");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
