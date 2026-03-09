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

public class StructureApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public StructureApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeStructureRepository);
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
    public async Task GetStructure_Anonymous_ShouldReturnStructure()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var structure = TournamentStructure.Create(tournament.Id);
        structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 2);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/structure");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TournamentStructureDto>();
        result.Should().NotBeNull();
        result!.Phases.Should().HaveCount(1);
        result.Phases![0].Name.Should().Be("Group Stage");
        result.Phases[0].Groups.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStructure_NoStructure_ShouldReturnEmptyStructure()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns((TournamentStructure?)null);
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/structure");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TournamentStructureDto>();
        result.Should().NotBeNull();
        result!.Phases.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStructure_NonExistentTournament_ShouldReturn404()
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync("nonexistent"))
            .Returns((Tournament?)null);
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tournaments/nonexistent/structure");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
