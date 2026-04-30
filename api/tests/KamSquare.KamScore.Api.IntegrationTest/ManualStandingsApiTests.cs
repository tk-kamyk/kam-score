using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class ManualStandingsApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public ManualStandingsApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeStructureRepository);
        Fake.Reset(factory.FakeTeamRepository);
        Fake.Reset(factory.FakeGameRepository);
    }

    private Tournament CreateTestTournament(string ownerId = "alice")
        => Tournament.Create("Summer Cup", Discipline.Volleyball, ownerId);

    private (TournamentStructure structure, Phase phase, Group group) CreateCustomStructure(
        string tournamentId, PhaseStatus status, params string[] teamIds)
    {
        var structure = TournamentStructure.Create(tournamentId);
        var phase = structure.AddPhase("Manual", PhaseFormat.Custom, numberOfGroups: 1);
        var group = phase.Groups[0];
        foreach (var id in teamIds) group.AddTeam(id);

        switch (status)
        {
            case PhaseStatus.InProgress:
                phase.Activate();
                break;
            case PhaseStatus.Completed:
                phase.Activate();
                phase.Complete();
                break;
        }

        return (structure, phase, group);
    }

    private List<Team> CreateTeams(string tournamentId, int count)
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

    private void SetupFakes(Tournament tournament, TournamentStructure structure, List<Team> teams)
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(teams);
        A.CallTo(() => _factory.FakeStructureRepository.UpdateAsync(A<TournamentStructure>._))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));
    }

    [Fact]
    public async Task Put_Standings_Owner_ReturnsOrderedStandings()
    {
        var tournament = CreateTestTournament();
        var (structure, phase, group) = CreateCustomStructure(
            tournament.Id, PhaseStatus.InProgress, "team1", "team2", "team3");
        SetupFakes(tournament, structure, CreateTeams(tournament.Id, 3));
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new UpdateManualStandingsDto(phase.Id, group.Id, ["team3", "team1", "team2"]);
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/standings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var standings = (await response.Content.ReadFromJsonAsync<List<StandingDto>>())!;
        standings.Single(s => s.TeamId == "team3").Position.Should().Be(1);
        standings.Single(s => s.TeamId == "team3").TeamName.Should().Be("Team 3");
        group.ManualStandingOrder.Should().BeEquivalentTo(
            ["team3", "team1", "team2"], opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task Put_Standings_NonOwner_Returns403()
    {
        var tournament = CreateTestTournament("alice");
        var (structure, phase, group) = CreateCustomStructure(
            tournament.Id, PhaseStatus.InProgress, "team1", "team2");
        SetupFakes(tournament, structure, CreateTeams(tournament.Id, 2));
        var client = _factory.CreateAuthenticatedClient("bob");

        var dto = new UpdateManualStandingsDto(phase.Id, group.Id, ["team1", "team2"]);
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/standings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_Standings_Anonymous_Returns401()
    {
        var tournament = CreateTestTournament();
        var (structure, phase, group) = CreateCustomStructure(
            tournament.Id, PhaseStatus.InProgress, "team1", "team2");
        SetupFakes(tournament, structure, CreateTeams(tournament.Id, 2));
        var client = _factory.CreateClient();

        var dto = new UpdateManualStandingsDto(phase.Id, group.Id, ["team1", "team2"]);
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/standings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public static TheoryData<string[]> InvalidOrderings => new()
    {
        // Foreign team ID.
        new[] { "team1", "team-other", "team3" },
        // Duplicate team ID.
        new[] { "team1", "team1", "team3" },
        // Fewer team IDs than the group has.
        new[] { "team1", "team2" },
        // More team IDs than the group has.
        new[] { "team1", "team2", "team3", "team1" },
    };

    [Theory]
    [MemberData(nameof(InvalidOrderings))]
    public async Task Put_Standings_InvalidOrdering_Returns400(string[] ordering)
    {
        var tournament = CreateTestTournament();
        var (structure, phase, group) = CreateCustomStructure(
            tournament.Id, PhaseStatus.InProgress, "team1", "team2", "team3");
        SetupFakes(tournament, structure, CreateTeams(tournament.Id, 3));
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new UpdateManualStandingsDto(phase.Id, group.Id, ordering.ToList());
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/standings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_Standings_NotCustomFormat_Returns400()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Pool", PhaseFormat.RoundRobin, numberOfGroups: 1);
        var group = phase.Groups[0];
        group.AddTeam("team1");
        group.AddTeam("team2");
        phase.Activate();
        SetupFakes(tournament, structure, CreateTeams(tournament.Id, 2));
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new UpdateManualStandingsDto(phase.Id, group.Id, ["team1", "team2"]);
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/standings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(PhaseStatus.New)]
    [InlineData(PhaseStatus.Completed)]
    public async Task Put_Standings_PhaseNotInProgress_Returns409(PhaseStatus status)
    {
        var tournament = CreateTestTournament();
        var (structure, phase, group) = CreateCustomStructure(
            tournament.Id, status, "team1", "team2");
        phase.Groups[0].ManualStandingOrder = status == PhaseStatus.Completed
            ? ["team1", "team2"]  // Completed phases must have a saved order.
            : [];
        SetupFakes(tournament, structure, CreateTeams(tournament.Id, 2));
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new UpdateManualStandingsDto(phase.Id, group.Id, ["team2", "team1"]);
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/standings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
