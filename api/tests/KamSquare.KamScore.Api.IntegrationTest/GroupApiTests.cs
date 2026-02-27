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

public class GroupApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public GroupApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeStructureRepository);
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
    public async Task AddGroup_Authenticated_ShouldReturnCreated()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new GroupDto(null, "D");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<GroupDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("D");
        result.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AddGroup_DuplicateName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new GroupDto(null, "A");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        doc.RootElement.GetProperty("errors").GetProperty("Name").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task AddGroup_DuplicateNameCaseInsensitive_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new GroupDto(null, "a");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateGroup_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var groupId = phase.Groups[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new GroupDto(null, "Pool 1");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GroupDto>();
        result!.Name.Should().Be("Pool 1");
    }

    [Fact]
    public async Task UpdateGroup_DuplicateName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var groupBId = phase.Groups[1].Id;
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new GroupDto(null, "A");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupBId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateGroup_SameName_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var groupId = phase.Groups[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new GroupDto(null, "A");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteGroup_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var groupId = phase.Groups[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        A.CallTo(() => _factory.FakeStructureRepository.UpdateAsync(A<TournamentStructure>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AddGroup_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("bob");

        var dto = new GroupDto(null, "D");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddGroup_EmptyName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new GroupDto(null, "");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteGroup_NonExistentGroup_ShouldReturn404()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
