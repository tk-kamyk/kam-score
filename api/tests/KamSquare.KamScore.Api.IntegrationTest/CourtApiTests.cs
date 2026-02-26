using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class CourtApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public CourtApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeCourtRepository);
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
    public async Task CreateCourt_Authenticated_ShouldReturnCreated()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("alice");
        A.CallTo(() => _factory.FakeCourtRepository.CreateAsync(A<Court>.Ignored))
            .ReturnsLazily((Court c) => Task.FromResult(c));

        var dto = new CourtDto(null, "Court A");
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/courts", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CourtDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Court A");
    }

    [Fact]
    public async Task UpdateCourt_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var court = Court.Create("Court A", tournament.Id);
        A.CallTo(() => _factory.FakeCourtRepository.GetByIdAsync(court.Id, tournament.Id))
            .Returns(court);
        A.CallTo(() => _factory.FakeCourtRepository.UpdateAsync(A<Court>.Ignored))
            .ReturnsLazily((Court c) => Task.FromResult(c));
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new CourtDto(null, "Main Court");
        var response = await client.PutAsJsonAsync($"/api/tournaments/{tournament.Id}/courts/{court.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CourtDto>();
        result!.Name.Should().Be("Main Court");
    }

    [Fact]
    public async Task DeleteCourt_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var court = Court.Create("Court A", tournament.Id);
        A.CallTo(() => _factory.FakeCourtRepository.GetByIdAsync(court.Id, tournament.Id))
            .Returns(court);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}/courts/{court.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        A.CallTo(() => _factory.FakeCourtRepository.DeleteAsync(court.Id, tournament.Id))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CreateCourt_DuplicateName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeCourtRepository.ExistsByNameAsync(tournament.Id, "Court A", null))
            .Returns(true);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new CourtDto(null, "Court A");
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/courts", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCourt_DuplicateName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var court2 = Court.Create("Court B", tournament.Id);
        A.CallTo(() => _factory.FakeCourtRepository.GetByIdAsync(court2.Id, tournament.Id))
            .Returns(court2);
        A.CallTo(() => _factory.FakeCourtRepository.ExistsByNameAsync(tournament.Id, "Court A", court2.Id))
            .Returns(true);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new CourtDto(null, "Court A");
        var response = await client.PutAsJsonAsync($"/api/tournaments/{tournament.Id}/courts/{court2.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCourt_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("bob");

        var dto = new CourtDto(null, "Court A");
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/courts", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCourts_Anonymous_ShouldReturnCourts()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var court = Court.Create("Court A", tournament.Id);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { court });
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/courts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CourtDto>>();
        result.Should().HaveCount(1);
        result![0].Name.Should().Be("Court A");
    }

    [Fact]
    public async Task CreateCourt_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var dto = new CourtDto(null, "Court A");
        var response = await client.PostAsJsonAsync("/api/tournaments/some-id/courts", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCourt_EmptyName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new CourtDto(null, "");
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/courts", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.GetProperty("title").GetString().Should().Be("Validation Error");
        root.GetProperty("errors").GetProperty("Name").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateCourt_DuplicateName_ShouldReturnFieldErrorForName()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeCourtRepository.ExistsByNameAsync(tournament.Id, "Court A", null))
            .Returns(true);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new CourtDto(null, "Court A");
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/courts", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        doc.RootElement.GetProperty("errors").GetProperty("Name").GetArrayLength().Should().Be(1);
    }
}
