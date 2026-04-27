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
        Fake.Reset(factory.FakeGameRepository);
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
    public async Task DeletePhase_Phase2_ShouldDeleteUpstreamPlaceholders()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase1 = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, groupWinners: 2);
        var phase2 = structure.AddPhase("Playoffs", PhaseFormat.PlayoffElimination, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase2.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Should delete placeholders sourced from phase1 (created FOR phase2)
        A.CallTo(() => _factory.FakeTeamRepository.DeleteBySourcePhaseIdAsync(
            tournament.Id, phase1.Id))
            .MustHaveHappenedOnceExactly();
        // Should also delete placeholders sourced from phase2 (created FOR any next phase)
        A.CallTo(() => _factory.FakeTeamRepository.DeleteBySourcePhaseIdAsync(
            tournament.Id, phase2.Id))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeletePhase_Phase1_ShouldNotDeleteUpstreamPlaceholders()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase1 = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Only one call for phase1's own downstream placeholders, no upstream
        A.CallTo(() => _factory.FakeTeamRepository.DeleteBySourcePhaseIdAsync(
            tournament.Id, phase1.Id))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _factory.FakeTeamRepository.DeleteBySourcePhaseIdAsync(
            A<string>.Ignored, A<string>.Ignored))
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

    [Fact]
    public async Task AddPhase_WithProgressionFields_ShouldReturnThem()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Groups", "RoundRobin",
            NumberOfGroups: 4, GroupWinners: 1, TotalTeamsProceeding: 6);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.GroupWinners.Should().Be(1);
        result.TotalTeamsProceeding.Should().Be(6);
    }

    [Fact]
    public async Task UpdatePhase_ProgressionFields_ShouldUpdate()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Groups", "RoundRobin",
            GroupWinners: 2, TotalTeamsProceeding: 8);
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.GroupWinners.Should().Be(2);
        result.TotalTeamsProceeding.Should().Be(8);
    }

    [Fact]
    public async Task AddPhase_WithLevels_ShouldReturnCreatedWithLevels()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Group Stage", "RoundRobin",
            NumberOfGroups: 3, NumberOfLevels: 2);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.Levels.Should().HaveCount(2);
        result.Groups.Should().HaveCount(6); // 2 levels * 3 groups
        result.Groups!.All(g => g.LevelId != null).Should().BeTrue();
    }

    [Fact]
    public async Task AddPhase_WithZeroProgression_ShouldReturnThem()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Final Round", "RoundRobin",
            NumberOfGroups: 2, GroupWinners: 0, TotalTeamsProceeding: 0);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.GroupWinners.Should().Be(0);
        result.TotalTeamsProceeding.Should().Be(0);
    }

    [Fact]
    public async Task UpdatePhase_ZeroProgression_ShouldUpdate()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Groups", "RoundRobin",
            GroupWinners: 0, TotalTeamsProceeding: 0);
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.GroupWinners.Should().Be(0);
        result.TotalTeamsProceeding.Should().Be(0);
    }

    [Fact]
    public async Task AddPhase_WithStartTime_ShouldReturnIt()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Groups", "RoundRobin",
            NumberOfGroups: 2, StartTime: "09:30");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.StartTime.Should().Be("09:30");
    }

    [Fact]
    public async Task UpdatePhase_StartTime_ShouldUpdate()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Groups", "RoundRobin", StartTime: "14:00");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.StartTime.Should().Be("14:00");
    }

    // Phase 2+ auto-assign uses seed ordering (not Random.Shared), so these tests
    // assert membership rather than specific group placement.
    [Fact]
    public async Task AutoAssign_Phase2_WithResolvedPlaceholders_ShouldUseRealTeamIds()
    {
        var (tournament, phase1, phase2) = SetupTwoPhaseAutoAssignScenario();

        var realTeam1 = Team.Create("Eagles", 90, tournament.Id);
        var realTeam2 = Team.Create("Hawks", 80, tournament.Id);

        var placeholder1 = Team.CreatePlaceholder("Groups - Seed 1", tournament.Id, phase1.Id, 1);
        placeholder1.ResolvedTeamId = realTeam1.Id;
        var placeholder2 = Team.CreatePlaceholder("Groups - Seed 2", tournament.Id, phase1.Id, 2);
        placeholder2.ResolvedTeamId = realTeam2.Id;

        A.CallTo(() => _factory.FakeTeamRepository.GetBySourcePhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(new List<Team> { placeholder1, placeholder2 });

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase2.Id}/auto-assign", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        var allTeamIds = result!.Groups!.SelectMany(g => g.TeamIds ?? []).ToList();
        allTeamIds.Should().HaveCount(2);
        allTeamIds.Should().Contain(realTeam1.Id);
        allTeamIds.Should().Contain(realTeam2.Id);
        allTeamIds.Should().NotContain(placeholder1.Id);
        allTeamIds.Should().NotContain(placeholder2.Id);
    }

    [Fact]
    public async Task AutoAssign_Phase2_WithUnresolvedPlaceholders_ShouldUsePlaceholderIds()
    {
        var (tournament, phase1, phase2) = SetupTwoPhaseAutoAssignScenario();

        var placeholder1 = Team.CreatePlaceholder("Groups - Seed 1", tournament.Id, phase1.Id, 1);
        var placeholder2 = Team.CreatePlaceholder("Groups - Seed 2", tournament.Id, phase1.Id, 2);

        A.CallTo(() => _factory.FakeTeamRepository.GetBySourcePhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(new List<Team> { placeholder1, placeholder2 });

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase2.Id}/auto-assign", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        var allTeamIds = result!.Groups!.SelectMany(g => g.TeamIds ?? []).ToList();
        allTeamIds.Should().HaveCount(2);
        allTeamIds.Should().Contain(placeholder1.Id);
        allTeamIds.Should().Contain(placeholder2.Id);
    }

    [Fact]
    public async Task AutoAssign_Phase2_WithMixedPlaceholders_ShouldUseBestAvailableId()
    {
        var (tournament, phase1, phase2) = SetupTwoPhaseAutoAssignScenario();

        var realTeam1 = Team.Create("Eagles", 90, tournament.Id);

        var placeholder1 = Team.CreatePlaceholder("Groups - Seed 1", tournament.Id, phase1.Id, 1);
        placeholder1.ResolvedTeamId = realTeam1.Id;
        var placeholder2 = Team.CreatePlaceholder("Groups - Seed 2", tournament.Id, phase1.Id, 2);
        // placeholder2 remains unresolved

        A.CallTo(() => _factory.FakeTeamRepository.GetBySourcePhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(new List<Team> { placeholder1, placeholder2 });

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase2.Id}/auto-assign", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        var allTeamIds = result!.Groups!.SelectMany(g => g.TeamIds ?? []).ToList();
        allTeamIds.Should().HaveCount(2);
        allTeamIds.Should().Contain(realTeam1.Id);
        allTeamIds.Should().Contain(placeholder2.Id);
    }

    [Fact]
    public async Task DeletePhase_MiddlePhase_ShouldRegeneratePlaceholdersForSuccessor()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase1 = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, groupWinners: 2);
        var phase2 = structure.AddPhase("Semis", PhaseFormat.PlayoffElimination, 1, groupWinners: 1);
        var phase3 = structure.AddPhase("Finals", PhaseFormat.PlayoffElimination, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase2.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Should create new placeholders sourced from phase1 for the successor phase (Finals)
        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(
            A<List<Team>>.That.Matches(teams =>
                teams.All(t => t.IsPlaceholder && t.SourcePhaseId == phase1.Id))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeletePhase_MiddlePhase_ShouldDeleteGamesAndClearGroupsForSuccessor()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase1 = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, groupWinners: 2);
        var phase2 = structure.AddPhase("Semis", PhaseFormat.PlayoffElimination, 1, groupWinners: 1);
        var phase3 = structure.AddPhase("Finals", PhaseFormat.PlayoffElimination, 1);
        // Assign a placeholder to phase3's group to verify it gets cleared
        var placeholder = Team.CreatePlaceholder("Semis - Seed 1", tournament.Id, phase2.Id, 1);
        structure.AssignTeam(phase3.Id, phase3.Groups[0].Id, placeholder.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase2.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Should delete games for the successor phase
        A.CallTo(() => _factory.FakeGameRepository.DeleteByPhaseIdAsync(
            tournament.Id, phase3.Id))
            .MustHaveHappenedOnceExactly();
        // Verify groups were cleared via structure update
        A.CallTo(() => _factory.FakeStructureRepository.UpdateAsync(
            A<TournamentStructure>.That.Matches(s =>
                s.Phases.First(p => p.Name == "Finals").Groups
                    .All(g => g.TeamIds.Count == 0))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeletePhase_FirstOfThree_ShouldNotRegeneratePlaceholders()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase1 = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, groupWinners: 2);
        var phase2 = structure.AddPhase("Semis", PhaseFormat.PlayoffElimination, 1, groupWinners: 1);
        var phase3 = structure.AddPhase("Finals", PhaseFormat.PlayoffElimination, 1);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Should NOT create new placeholders (Semis is now first phase)
        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(
            A<List<Team>>.Ignored))
            .MustNotHaveHappened();
        // Should still delete games for successor phase and clear groups
        A.CallTo(() => _factory.FakeGameRepository.DeleteByPhaseIdAsync(
            tournament.Id, phase2.Id))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AutoAssign_WithLevels_ShouldSplitTeamsByLevel()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        SetupTournamentAndStructure(tournament, structure);
        var teams = new[]
        {
            Team.Create("Team1", 90, tournament.Id),
            Team.Create("Team2", 80, tournament.Id),
            Team.Create("Team3", 70, tournament.Id),
            Team.Create("Team4", 60, tournament.Id),
            Team.Create("Team5", 50, tournament.Id),
            Team.Create("Team6", 40, tournament.Id),
            Team.Create("Team7", 30, tournament.Id),
            Team.Create("Team8", 20, tournament.Id),
        };
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(teams);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/auto-assign", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();

        // Level 1 groups (first 2) should have the top-seeded teams (highest level values)
        var level1GroupIds = phase.Groups
            .Where(g => g.LevelId == phase.Levels[0].Id)
            .Select(g => g.Id).ToHashSet();
        var level1TeamIds = result!.Groups!
            .Where(g => level1GroupIds.Contains(g.Id!))
            .SelectMany(g => g.TeamIds ?? []).ToList();
        // Top 4 teams by level desc: Team1(90), Team2(80), Team3(70), Team4(60)
        level1TeamIds.Should().HaveCount(4);

        var level2GroupIds = phase.Groups
            .Where(g => g.LevelId == phase.Levels[1].Id)
            .Select(g => g.Id).ToHashSet();
        var level2TeamIds = result.Groups!
            .Where(g => level2GroupIds.Contains(g.Id!))
            .SelectMany(g => g.TeamIds ?? []).ToList();
        // Bottom 4 teams: Team5(50), Team6(40), Team7(30), Team8(20)
        level2TeamIds.Should().HaveCount(4);
    }

    [Fact]
    public async Task UpdatePhase_ChangedProgression_ShouldRegeneratePlaceholders()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase1 = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, groupWinners: 1);
        var phase2 = structure.AddPhase("Finals", PhaseFormat.PlayoffElimination, 1);
        SetupTournamentAndStructure(tournament, structure);

        // Setup existing placeholders from phase1
        var existingPlaceholders = Enumerable.Range(1, 2)
            .Select(seed => Team.CreatePlaceholder($"Groups - Seed {seed}", tournament.Id, phase1.Id, seed))
            .ToList();
        // Assign them to phase2
        foreach (var p in existingPlaceholders)
            phase2.Groups[0].AddTeam(p.Id);

        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(A<IEnumerable<Team>>.Ignored))
            .ReturnsLazily((IEnumerable<Team> teams) => Task.FromResult(teams));

        var client = _factory.CreateAuthenticatedClient("alice");

        // Update phase1 with new groupWinners (1 -> 2), which changes progression
        var dto = new PhaseDto(null, "Groups", "RoundRobin", GroupWinners: 2);
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Old placeholders should be deleted
        A.CallTo(() => _factory.FakeTeamRepository.DeleteBySourcePhaseIdAsync(
            tournament.Id, phase1.Id))
            .MustHaveHappenedOnceExactly();

        // Old next phase games should be deleted
        A.CallTo(() => _factory.FakeGameRepository.DeleteByPhaseIdAsync(
            tournament.Id, phase2.Id))
            .MustHaveHappenedOnceExactly();

        // New placeholders should be created (4 = 2 groups × 2 winners)
        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(
            A<List<Team>>.That.Matches(teams =>
                teams.Count == 4 &&
                teams.All(t => t.IsPlaceholder && t.SourcePhaseId == phase1.Id))))
            .MustHaveHappenedOnceExactly();
    }

    // --- Cascading Level Constraint ---

    [Fact]
    public async Task AddPhase_FewerLevelsThanPrevious_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Playoffs", "PlayoffElimination",
            NumberOfGroups: 1, NumberOfLevels: 1);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPhase_NonMultipleLevels_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Playoffs", "PlayoffElimination",
            NumberOfGroups: 1, NumberOfLevels: 3);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPhase_NoLevelsAfterLeveledPhase_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Playoffs", "PlayoffElimination", NumberOfGroups: 1);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPhase_MultipleLevels_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, numberOfLevels: 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Playoffs", "PlayoffElimination",
            NumberOfGroups: 1, NumberOfLevels: 4);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.Levels.Should().HaveCount(4);
    }

    [Fact]
    public async Task AddPhase_IntroduceLevelsAfterNoLevels_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Playoffs", "PlayoffElimination",
            NumberOfGroups: 1, NumberOfLevels: 2);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.Levels.Should().HaveCount(2);
    }

    // phase1 is phase2's previous phase because it was added first (Order 1 vs Order 2)
    private (Tournament tournament, Phase phase1, Phase phase2) SetupTwoPhaseAutoAssignScenario()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase1 = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2, groupWinners: 1);
        var phase2 = structure.AddPhase("Finals", PhaseFormat.PlayoffElimination, 1);
        SetupTournamentAndStructure(tournament, structure);
        return (tournament, phase1, phase2);
    }

    // --- Custom phase format ---

    [Fact]
    public async Task AddPhase_Custom_ShouldCreate()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Manual Phase", "Custom", NumberOfGroups: 2);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.Format.Should().Be("Custom");
        result.Groups.Should().HaveCount(2);
        result.Status.Should().Be("New");
    }

    [Fact]
    public async Task UpdatePhase_CustomToRoundRobin_ClearsManualStandings()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTestStructure(tournament.Id);
        var phase = structure.AddPhase("Manual", PhaseFormat.Custom, 1);
        phase.Groups[0].AddTeam("t1");
        phase.Groups[0].AddTeam("t2");
        phase.Groups[0].ManualStandingOrder = ["t2", "t1"];
        SetupTournamentAndStructure(tournament, structure);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Manual", "RoundRobin");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        phase.Groups[0].ManualStandingOrder.Should().BeEmpty();
    }
}
