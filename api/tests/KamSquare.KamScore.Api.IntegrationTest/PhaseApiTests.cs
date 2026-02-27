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

public class PhaseApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public PhaseApiTests(KamScoreWebApplicationFactory factory)
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

    private TournamentStructure CreateTestStructure(string tournamentId)
    {
        return TournamentStructure.Create(tournamentId);
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
    public async Task AddPhase_Authenticated_ShouldReturnCreated()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Group Stage", "RoundRobin", NumberOfGroups: 3);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Group Stage");
        result.Format.Should().Be("RoundRobin");
        result.Order.Should().Be(1);
        result.Groups.Should().HaveCount(3);
        result.Groups![0].Name.Should().Be("A");
        result.Groups[1].Name.Should().Be("B");
        result.Groups[2].Name.Should().Be("C");
    }

    [Fact]
    public async Task AddPhase_DefaultNumberOfGroups_ShouldCreate1Group()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Finals", "PlayoffElimination");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.Groups.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddPhase_MultiplePhasesHaveSequentialOrder()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto1 = new PhaseDto(null, "Groups", "RoundRobin", NumberOfGroups: 2);
        await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/structure/phases", dto1);

        var dto2 = new PhaseDto(null, "Playoffs", "PlayoffElimination");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto2);

        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.Order.Should().Be(2);
    }

    [Fact]
    public async Task AddPhase_EmptyName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "", "RoundRobin");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
        doc.RootElement.GetProperty("errors").GetProperty("Name").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddPhase_InvalidFormat_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Groups", "InvalidFormat");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPhase_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("bob");

        var dto = new PhaseDto(null, "Groups", "RoundRobin");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddPhase_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var dto = new PhaseDto(null, "Groups", "RoundRobin");
        var response = await client.PostAsJsonAsync(
            "/api/tournaments/some-id/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddPhase_NoStructure_ShouldReturn404()
    {
        var tournament = CreateTestTournament();
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns((TournamentStructure?)null);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Groups", "RoundRobin");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePhase_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Pool Stage", "PlayoffWithPlacement");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.Name.Should().Be("Pool Stage");
        result.Format.Should().Be("PlayoffWithPlacement");
    }

    [Fact]
    public async Task UpdatePhase_NonExistentPhase_ShouldReturn404()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Updated", "RoundRobin");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/nonexistent", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePhase_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        A.CallTo(() => _factory.FakeStructureRepository.UpdateAsync(A<TournamentStructure>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeletePhase_ShouldReorderRemainingPhases()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var semis = structure.AddPhase("Semis", PhaseFormat.PlayoffElimination, 1);
        structure.AddPhase("Finals", PhaseFormat.PlayoffElimination, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{semis.Id}");

        // Verify reordering happened by checking the structure was updated
        A.CallTo(() => _factory.FakeStructureRepository.UpdateAsync(
            A<TournamentStructure>.That.Matches(s =>
                s.Phases.Count == 2 &&
                s.Phases[0].Name == "Groups" && s.Phases[0].Order == 1 &&
                s.Phases[1].Name == "Finals" && s.Phases[1].Order == 2)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeletePhase_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("bob");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AutoAssign_ShouldDistributeTeamsByLevel()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        SetupTournamentAndStructure(tournament, structure);
        var teams = new[]
        {
            Team.Create("Eagles", 90, tournament.Id),
            Team.Create("Hawks", 80, tournament.Id),
            Team.Create("Falcons", 70, tournament.Id),
            Team.Create("Ravens", 60, tournament.Id),
        };
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(teams);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/auto-assign", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result.Should().NotBeNull();
        // Level order: Eagles(90), Hawks(80), Falcons(70), Ravens(60)
        // Snake: Eagles→A, Hawks→B, Ravens→B, Falcons→A
        result!.Groups![0].TeamIds.Should().HaveCount(2);
        result.Groups[1].TeamIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task AutoAssign_ShouldClearExistingAssignments()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var oldTeam = Team.Create("OldTeam", 50, tournament.Id);
        structure.AssignTeam(phase.Id, phase.Groups[0].Id, oldTeam.Id);
        SetupTournamentAndStructure(tournament, structure);
        var newTeam = Team.Create("NewTeam", 80, tournament.Id);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { newTeam });
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/auto-assign", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        var allTeamIds = result!.Groups!.SelectMany(g => g.TeamIds ?? []).ToList();
        allTeamIds.Should().Contain(newTeam.Id);
        allTeamIds.Should().NotContain(oldTeam.Id);
    }

    [Fact]
    public async Task AutoAssign_NoGroups_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        // Remove the only group to make it empty
        structure.RemoveGroup(phase.Id, phase.Groups[0].Id);
        SetupTournamentAndStructure(tournament, structure);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { Team.Create("Eagles", 50, tournament.Id) });
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/auto-assign", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AutoAssign_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("bob");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/auto-assign", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AutoAssign_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/tournaments/t1/structure/phases/p1/auto-assign", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AutoAssign_NoStructure_ShouldReturn404()
    {
        var tournament = CreateTestTournament();
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns((TournamentStructure?)null);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/some-phase/auto-assign", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
