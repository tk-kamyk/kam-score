using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class TournamentApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public TournamentApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeTeamRepository);
        Fake.Reset(factory.FakeCourtRepository);
        Fake.Reset(factory.FakeStructureRepository);
        Fake.Reset(factory.FakeGameRepository);
    }

    [Fact]
    public async Task CreateTournament_Authenticated_ShouldReturnCreated()
    {
        var client = _factory.CreateAuthenticatedClient("alice");
        A.CallTo(() => _factory.FakeRepository.CreateAsync(A<Tournament>.Ignored))
            .ReturnsLazily((Tournament t) => Task.FromResult(t));

        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, null, null, null);

        var response = await client.PostAsJsonAsync("/api/tournaments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Summer Cup");
        result.TournamentCode.Should().NotBeNullOrEmpty();
        result.OwnerId.Should().Be("alice");
    }

    [Fact]
    public async Task CreateTournament_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, null, null, null);

        var response = await client.PostAsJsonAsync("/api/tournaments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTournaments_Anonymous_ShouldReturnAllWithoutCodes()
    {
        var client = _factory.CreateClient();
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetAllAsync())
            .Returns(new[] { tournament });

        var response = await client.GetAsync("/api/tournaments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TournamentDto>>();
        result.Should().HaveCount(1);
        result![0].TournamentCode.Should().BeNull();
    }

    [Fact]
    public async Task GetTournaments_Authenticated_ShouldReturnOwnWithCodes()
    {
        var client = _factory.CreateAuthenticatedClient("alice");
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByOwnerIdAsync("alice"))
            .Returns(new[] { tournament });

        var response = await client.GetAsync("/api/tournaments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TournamentDto>>();
        result.Should().HaveCount(1);
        result![0].TournamentCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTournament_Owner_ShouldIncludeCode()
    {
        var client = _factory.CreateAuthenticatedClient("alice");
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.TournamentCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTournament_Anonymous_ShouldExcludeCode()
    {
        var client = _factory.CreateClient();
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.TournamentCode.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTournament_Owner_ShouldSucceed()
    {
        var client = _factory.CreateAuthenticatedClient("alice");
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        A.CallTo(() => _factory.FakeRepository.UpdateAsync(A<Tournament>.Ignored))
            .ReturnsLazily((Tournament t) => Task.FromResult(t));

        var dto = new TournamentDto(null, "Winter Cup", "Volleyball", null, null, null, null, null);

        var response = await client.PutAsJsonAsync($"/api/tournaments/{tournament.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.Name.Should().Be("Winter Cup");
    }

    [Fact]
    public async Task UpdateTournament_NonOwner_ShouldReturn403()
    {
        var client = _factory.CreateAuthenticatedClient("bob");
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);

        var dto = new TournamentDto(null, "Winter Cup", "Volleyball", null, null, null, null, null);

        var response = await client.PutAsJsonAsync($"/api/tournaments/{tournament.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTournament_Owner_ShouldSucceed()
    {
        var client = _factory.CreateAuthenticatedClient("alice");
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        A.CallTo(() => _factory.FakeRepository.DeleteAsync(tournament.Id, "alice"))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteTournament_NonOwner_ShouldReturn403()
    {
        var client = _factory.CreateAuthenticatedClient("bob");
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTournament_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/tournaments/some-id");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTournament_WithGameConditions_ShouldStoreCorrectly()
    {
        var client = _factory.CreateAuthenticatedClient("alice");
        A.CallTo(() => _factory.FakeRepository.CreateAsync(A<Tournament>.Ignored))
            .ReturnsLazily((Tournament t) => Task.FromResult(t));

        var conditions = new GameConditionsDto(3, [21, 21, 15]);
        var dto = new TournamentDto(null, "Beach Cup", "BeachVolleyball", null, 45, conditions, null, null);

        var response = await client.PostAsJsonAsync("/api/tournaments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.GameConditions.Should().NotBeNull();
        result.GameConditions!.BestOfSets.Should().Be(3);
    }
}
