using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Api.Endpoints;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class TeamAssignmentApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public TeamAssignmentApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeStructureRepository);
        Fake.Reset(factory.FakeTeamRepository);
    }

    private Tournament CreateTestTournament(string ownerId = "alice")
    {
        return Tournament.Create("Summer Cup", Discipline.Volleyball, ownerId);
    }

    private void SetupTournamentAndStructure(Tournament tournament, TournamentStructure structure)
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);
        A.CallTo(() => _factory.FakeStructureRepository.UpdateAsync(A<TournamentStructure>.Ignored))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));
    }

    [Fact]
    public async Task AssignTeam_Authenticated_ShouldReturnCreated()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var groupId = phase.Groups[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        var team = Team.Create("Eagles", 50, tournament.Id);
        A.CallTo(() => _factory.FakeTeamRepository.GetByIdAsync(team.Id, tournament.Id))
            .Returns(team);
        var client = _factory.CreateAuthenticatedClient("alice");

        var request = new TeamAssignmentRequest(team.Id);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}/teams",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AssignTeam_NonExistentTeam_ShouldReturn404()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var groupId = phase.Groups[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        A.CallTo(() => _factory.FakeTeamRepository.GetByIdAsync("nonexistent", tournament.Id))
            .Returns((Team?)null);
        var client = _factory.CreateAuthenticatedClient("alice");

        var request = new TeamAssignmentRequest("nonexistent");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}/teams",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignTeam_DuplicateInPhase_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var groupAId = phase.Groups[0].Id;
        var groupBId = phase.Groups[1].Id;
        var team = Team.Create("Eagles", 50, tournament.Id);
        structure.AssignTeam(phase.Id, groupAId, team.Id);
        SetupTournamentAndStructure(tournament, structure);
        A.CallTo(() => _factory.FakeTeamRepository.GetByIdAsync(team.Id, tournament.Id))
            .Returns(team);
        var client = _factory.CreateAuthenticatedClient("alice");

        var request = new TeamAssignmentRequest(team.Id);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupBId}/teams",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveTeam_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var groupId = phase.Groups[0].Id;
        var team = Team.Create("Eagles", 50, tournament.Id);
        structure.AssignTeam(phase.Id, groupId, team.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}/teams/{team.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveTeam_NotAssigned_ShouldReturn404()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var groupId = phase.Groups[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}/teams/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignTeam_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var groupId = phase.Groups[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("bob");

        var request = new TeamAssignmentRequest("some-team-id");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}/teams",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignTeam_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var request = new TeamAssignmentRequest("some-team-id");
        var response = await client.PostAsJsonAsync(
            "/api/tournaments/t1/structure/phases/p1/groups/g1/teams", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
