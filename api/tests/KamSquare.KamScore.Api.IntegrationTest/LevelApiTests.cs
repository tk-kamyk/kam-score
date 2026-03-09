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

public class LevelApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public LevelApiTests(KamScoreWebApplicationFactory factory)
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
    public async Task AddPhase_WithLevels_ShouldReturnLevelsAndGroups()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Group Stage", "RoundRobin",
            NumberOfGroups: 2, NumberOfLevels: 2);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result.Should().NotBeNull();
        result!.Levels.Should().HaveCount(2);
        result.Levels![0].Name.Should().Be("Level 1");
        result.Levels![0].Order.Should().Be(1);
        result.Levels![1].Name.Should().Be("Level 2");
        result.Levels![1].Order.Should().Be(2);
        result.Groups.Should().HaveCount(4); // 2 levels * 2 groups
    }

    [Fact]
    public async Task AddPhase_WithoutLevels_ShouldReturnNoLevels()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Group Stage", "RoundRobin", NumberOfGroups: 2);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.Levels.Should().BeNullOrEmpty();
        result.Groups.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddPhase_WithLevels_GroupsHaveLevelId()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Group Stage", "RoundRobin",
            NumberOfGroups: 2, NumberOfLevels: 2);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        var level1Id = result!.Levels![0].Id;
        var level2Id = result.Levels[1].Id;

        // First 2 groups belong to level 1, next 2 to level 2
        result.Groups![0].LevelId.Should().Be(level1Id);
        result.Groups[1].LevelId.Should().Be(level1Id);
        result.Groups[2].LevelId.Should().Be(level2Id);
        result.Groups[3].LevelId.Should().Be(level2Id);
    }

    [Fact]
    public async Task UpdateLevel_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        var levelId = phase.Levels[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new LevelDto(null, "Gold");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/levels/{levelId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LevelDto>();
        result!.Name.Should().Be("Gold");
        result.Id.Should().Be(levelId);
        result.Order.Should().Be(1);
    }

    [Fact]
    public async Task UpdateLevel_DuplicateName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        var levelId = phase.Levels[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        // Try to rename Level 1 to "Level 2" which already exists
        var dto = new LevelDto(null, "Level 2");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/levels/{levelId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        doc.RootElement.GetProperty("errors").GetProperty("Name").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task UpdateLevel_SameName_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        var levelId = phase.Levels[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new LevelDto(null, "Level 1");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/levels/{levelId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateLevel_NonExistentLevel_ShouldReturn404()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new LevelDto(null, "Gold");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/levels/nonexistent", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateLevel_CompletedPhase_ShouldReturn409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        phase.Complete();
        var levelId = phase.Levels[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new LevelDto(null, "Gold");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/levels/{levelId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateLevel_NonExistentPhase_ShouldReturn404()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new LevelDto(null, "Gold");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/nonexistent/levels/some-level", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateLevel_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        var levelId = phase.Levels[0].Id;
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("bob");

        var dto = new LevelDto(null, "Gold");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/levels/{levelId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateLevel_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var dto = new LevelDto(null, "Gold");
        var response = await client.PutAsJsonAsync(
            "/api/tournaments/t1/structure/phases/p1/levels/l1", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
