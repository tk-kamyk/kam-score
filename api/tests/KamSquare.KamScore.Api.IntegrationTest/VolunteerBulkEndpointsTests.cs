using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class VolunteerBulkEndpointsTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public VolunteerBulkEndpointsTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeVolunteerRepository);
        Fake.Reset(factory.FakeStructureRepository);
        Fake.Reset(factory.FakeGameRepository);
    }

    private Tournament CreateTestTournament(string ownerId = "alice", int? gameLength = 20)
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, ownerId);
        tournament.Update("Summer Cup", Discipline.Volleyball, DateTime.UtcNow, gameLength, null);
        return tournament;
    }

    private TournamentStructure CreateStructureWithPhase(string tournamentId, string phaseName, TimeOnly startTime)
    {
        var structure = TournamentStructure.Create(tournamentId);
        structure.AddPhase(phaseName, PhaseFormat.RoundRobin, 1, startTime: startTime);
        return structure;
    }

    private void SetupTournamentAndStructure(Tournament tournament, TournamentStructure structure)
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Game>());
        A.CallTo(() => _factory.FakeVolunteerRepository.UpdateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));
    }

    // --- DELETE /shifts/{shiftGroup}/assignments ---

    [Fact]
    public async Task ClearShiftGroupAssignments_Owner_ShouldReturn204()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhase(tournament.Id, "Pool", new TimeOnly(9, 0));
        SetupTournamentAndStructure(tournament, structure);

        var volunteer = Volunteer.Create("Alice", tournament.Id);
        volunteer.AssignShift("Pool", new TimeOnly(9, 0));
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { volunteer });
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/assignments");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        volunteer.Assignments.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearShiftGroupAssignments_UnknownShiftGroup_ShouldReturn404()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhase(tournament.Id, "Pool", new TimeOnly(9, 0));
        SetupTournamentAndStructure(tournament, structure);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Volunteer>());
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}/volunteers/shifts/Unknown/assignments");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClearShiftGroupAssignments_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/tournaments/some-id/volunteers/shifts/Pool/assignments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClearShiftGroupAssignments_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament(ownerId: "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        var client = _factory.CreateAuthenticatedClient("eve");

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/assignments");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- POST /shifts/{shiftGroup}/auto-assign ---

    [Fact]
    public async Task AutoAssign_Owner_ShouldReturn200_AndAssignVolunteers()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhase(tournament.Id, "Pool", new TimeOnly(9, 0));
        SetupTournamentAndStructure(tournament, structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        var bob = Volunteer.Create("Bob", tournament.Id);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { alice, bob });
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/auto-assign",
            new AutoAssignShiftGroupDto(2));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Pool has a single shift (no next phase + one round) — both volunteers should land on it
        alice.Assignments.Should().ContainSingle(a => a.ShiftGroup == "Pool");
        bob.Assignments.Should().ContainSingle(a => a.ShiftGroup == "Pool");
    }

    // Wiring check only — exhaustive boundary cases live in AutoAssignShiftGroupDtoValidatorTests.
    [Fact]
    public async Task AutoAssign_InvalidVolunteersPerShift_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhase(tournament.Id, "Pool", new TimeOnly(9, 0));
        SetupTournamentAndStructure(tournament, structure);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Volunteer>());
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/auto-assign",
            new AutoAssignShiftGroupDto(0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AutoAssign_UnknownShiftGroup_ShouldReturn404()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhase(tournament.Id, "Pool", new TimeOnly(9, 0));
        SetupTournamentAndStructure(tournament, structure);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Volunteer>());
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/volunteers/shifts/Unknown/auto-assign",
            new AutoAssignShiftGroupDto(2));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AutoAssign_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/tournaments/some-id/volunteers/shifts/Pool/auto-assign",
            new AutoAssignShiftGroupDto(2));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AutoAssign_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament(ownerId: "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        var client = _factory.CreateAuthenticatedClient("eve");

        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/auto-assign",
            new AutoAssignShiftGroupDto(2));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
