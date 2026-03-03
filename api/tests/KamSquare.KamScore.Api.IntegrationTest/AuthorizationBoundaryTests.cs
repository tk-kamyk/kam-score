using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class AuthorizationBoundaryTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public AuthorizationBoundaryTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeTeamRepository);
        Fake.Reset(factory.FakeCourtRepository);
        Fake.Reset(factory.FakeStructureRepository);
        Fake.Reset(factory.FakeGameRepository);
    }

    private Tournament CreateTestTournament(string ownerId = "alice")
    {
        return Tournament.Create("Summer Cup", Discipline.Volleyball, ownerId);
    }

    private void SetupTournament(Tournament tournament)
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
    }

    [Fact]
    public async Task CreateTeam_NonOwner_Returns403()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("bob");

        var dto = new TeamDto(null, "Eagles", 75, null, null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/teams", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateCourt_NonOwner_Returns403()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("bob");

        var dto = new CourtDto(null, "Court B");
        var response = await client.PutAsJsonAsync($"/api/tournaments/{tournament.Id}/courts/some-court-id", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddPhase_NonOwner_Returns403()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("bob");

        var dto = new PhaseDto(null, "Group Stage", "RoundRobin", 1, null, null, null, null, []);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTournament_NonOwner_Returns403()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("bob");

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateTeam_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var dto = new TeamDto(null, "Eagles", 75, null, null);
        var response = await client.PostAsJsonAsync("/api/tournaments/some-id/teams", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTeam_TournamentCodeOnly_Returns401()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tournament-Code", tournament.TournamentCode);

        var dto = new TeamDto(null, "Eagles", 75, null, null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/teams", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
