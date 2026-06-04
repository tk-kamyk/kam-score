using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class VolunteerShiftApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public VolunteerShiftApiTests(KamScoreWebApplicationFactory factory)
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

    private TournamentStructure CreateStructureWithPhases(string tournamentId, params (string name, TimeOnly? startTime)[] phaseDefs)
    {
        var structure = TournamentStructure.Create(tournamentId);
        foreach (var (name, startTime) in phaseDefs)
        {
            structure.AddPhase(name, PhaseFormat.RoundRobin, 1, startTime: startTime);
        }
        return structure;
    }

    private void SetupTournamentAndStructure(Tournament tournament, TournamentStructure structure)
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);
    }

    // --- GET /shifts ---

    [Fact]
    public async Task GetShifts_Owner_ShouldReturnShiftGroups()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Volunteer>());
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Game>());
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/volunteers/shifts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ShiftGroupResponseDto>>();
        result.Should().NotBeNull();
        result!.Select(g => g.Name).Should().ContainInOrder("Set-up", "Pool", "Cleanup");
    }

    [Fact]
    public async Task GetShifts_ShouldIncludeAssignedVolunteersWithAvailability()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);
        var volunteer = Volunteer.Create("John Doe", tournament.Id, teamId: "team-1");
        volunteer.AssignShift("Pool", new TimeOnly(9, 0));
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { volunteer });
        var game = Game.Create(tournament.Id, structure.Phases[0].Id, structure.Phases[0].Groups[0].Id, 1,
            homeTeamId: "team-1", awayTeamId: "team-2");
        game.AssignSchedule("court-1", new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc));
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { game });
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/volunteers/shifts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ShiftGroupResponseDto>>();
        var poolGroup = result!.First(g => g.Name == "Pool");
        var firstShift = poolGroup.Shifts.First(s => s.ShiftTime == "09:00");
        firstShift.Volunteers.Should().ContainSingle();
        firstShift.Volunteers[0].Name.Should().Be("John Doe");
        firstShift.Volunteers[0].Available.Should().BeFalse();
    }

    [Fact]
    public async Task GetShifts_ShouldOrderVolunteersByStationThenName()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);

        // Uncoloured (Zoe) sorts last; station 0 (Carol) before station 1 (Bob); ties by name.
        var zoe = Volunteer.Create("Zoe", tournament.Id);
        zoe.AssignShift("Pool", new TimeOnly(9, 0));
        var carol = Volunteer.Create("Carol", tournament.Id);
        carol.AssignShift("Pool", new TimeOnly(9, 0));
        carol.SetStation("Pool", new TimeOnly(9, 0), 0);
        var bob = Volunteer.Create("Bob", tournament.Id);
        bob.AssignShift("Pool", new TimeOnly(9, 0));
        bob.SetStation("Pool", new TimeOnly(9, 0), 1);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { zoe, carol, bob });
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Game>());
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/volunteers/shifts");

        var result = await response.Content.ReadFromJsonAsync<List<ShiftGroupResponseDto>>();
        var slot = result!.First(g => g.Name == "Pool").Shifts.First(s => s.ShiftTime == "09:00");
        slot.Volunteers.Select(v => v.Name).Should().ContainInOrder("Carol", "Bob", "Zoe");
    }

    [Fact]
    public async Task GetShifts_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tournaments/some-id/volunteers/shifts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- GET /shifts/{shiftGroup}/{shiftTime}/available ---

    [Fact]
    public async Task GetAvailable_ShouldReturnSortedVolunteers()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);

        var vol1 = Volunteer.Create("Charlie", tournament.Id);
        vol1.AssignShift("Pool", new TimeOnly(9, 20));
        vol1.AssignShift("Pool", new TimeOnly(9, 40));
        var vol2 = Volunteer.Create("Alice", tournament.Id);
        vol2.AssignShift("Pool", new TimeOnly(9, 20));
        var vol3 = Volunteer.Create("Bob", tournament.Id, teamId: "team-1");
        vol3.AssignShift("Pool", new TimeOnly(9, 20));

        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { vol1, vol2, vol3 });

        var game = Game.Create(tournament.Id, structure.Phases[0].Id, structure.Phases[0].Groups[0].Id, 1,
            homeTeamId: "team-1", awayTeamId: "team-2");
        game.AssignSchedule("court-1", new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc));
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { game });

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/09:00/available");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<VolunteerAvailabilityDto>>();
        result.Should().NotBeNull();
        // Sorted: available first (fewest shifts), then unavailable
        result![0].Name.Should().Be("Alice");
        result[1].Name.Should().Be("Charlie");
        result[2].Name.Should().Be("Bob");
        result[2].Available.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailable_ShouldShowPlaysBefore()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);

        var volunteer = Volunteer.Create("John", tournament.Id, teamId: "team-1");
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { volunteer });

        var game = Game.Create(tournament.Id, structure.Phases[0].Id, structure.Phases[0].Groups[0].Id, 1,
            homeTeamId: "team-1", awayTeamId: "team-2");
        game.AssignSchedule("court-1", new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc));
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { game });

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/09:20/available");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<VolunteerAvailabilityDto>>();
        result!.First().PlaysBefore.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailable_ShouldShowPlaysAfter()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);

        var volunteer = Volunteer.Create("John", tournament.Id, teamId: "team-1");
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { volunteer });

        var game = Game.Create(tournament.Id, structure.Phases[0].Id, structure.Phases[0].Groups[0].Id, 1,
            homeTeamId: "team-1", awayTeamId: "team-2");
        game.AssignSchedule("court-1", new DateTime(2026, 6, 15, 9, 20, 0, DateTimeKind.Utc));
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { game });

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/09:00/available");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<VolunteerAvailabilityDto>>();
        result!.First().PlaysAfter.Should().BeTrue();
    }

    // --- POST assign ---

    [Fact]
    public async Task AssignVolunteer_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);

        var volunteer = Volunteer.Create("John", tournament.Id);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByIdAsync(volunteer.Id, tournament.Id))
            .Returns(volunteer);
        A.CallTo(() => _factory.FakeVolunteerRepository.UpdateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Game>());
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/09:00/assign/{volunteer.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignVolunteer_InvalidShiftTime_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);

        var volunteer = Volunteer.Create("John", tournament.Id);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByIdAsync(volunteer.Id, tournament.Id))
            .Returns(volunteer);
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Game>());
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/09:15/assign/{volunteer.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignVolunteerToSpecialShift_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);

        var volunteer = Volunteer.Create("John", tournament.Id);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByIdAsync(volunteer.Id, tournament.Id))
            .Returns(volunteer);
        A.CallTo(() => _factory.FakeVolunteerRepository.UpdateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/volunteers/shifts/Set-up/assign/{volunteer.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignVolunteer_WithStationBody_PersistsAndSurfacesOnGetShifts()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);

        var volunteer = Volunteer.Create("John", tournament.Id);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByIdAsync(volunteer.Id, tournament.Id))
            .Returns(volunteer);
        A.CallTo(() => _factory.FakeVolunteerRepository.UpdateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { volunteer });
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Game>());
        var client = _factory.CreateAuthenticatedClient("alice");

        var assign = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/09:00/assign/{volunteer.Id}",
            new AssignShiftRequestDto(2));
        assign.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/volunteers/shifts");
        var result = await response.Content.ReadFromJsonAsync<List<ShiftGroupResponseDto>>();
        var firstShift = result!.First(g => g.Name == "Pool").Shifts.First(s => s.ShiftTime == "09:00");
        firstShift.Volunteers.Should().ContainSingle().Which.Station.Should().Be(2);
    }

    [Fact]
    public async Task AssignVolunteer_BareReassign_KeepsExistingStation()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);

        var volunteer = Volunteer.Create("John", tournament.Id);
        volunteer.AssignShift("Pool", new TimeOnly(9, 0));
        volunteer.SetStation("Pool", new TimeOnly(9, 0), 4);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByIdAsync(volunteer.Id, tournament.Id))
            .Returns(volunteer);
        A.CallTo(() => _factory.FakeVolunteerRepository.UpdateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { volunteer });
        A.CallTo(() => _factory.FakeGameRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Game>());
        var client = _factory.CreateAuthenticatedClient("alice");

        // Bare re-assign — no body — must not clear the colour.
        var assign = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/09:00/assign/{volunteer.Id}", null);
        assign.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/volunteers/shifts");
        var result = await response.Content.ReadFromJsonAsync<List<ShiftGroupResponseDto>>();
        var firstShift = result!.First(g => g.Name == "Pool").Shifts.First(s => s.ShiftTime == "09:00");
        firstShift.Volunteers.Should().ContainSingle().Which.Station.Should().Be(4);
    }

    // --- DELETE unassign ---

    [Fact]
    public async Task UnassignVolunteer_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);

        var volunteer = Volunteer.Create("John", tournament.Id);
        volunteer.AssignShift("Pool", new TimeOnly(9, 0));
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByIdAsync(volunteer.Id, tournament.Id))
            .Returns(volunteer);
        A.CallTo(() => _factory.FakeVolunteerRepository.UpdateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/volunteers/shifts/Pool/09:00/assign/{volunteer.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnassignVolunteerFromSpecialShift_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        var structure = CreateStructureWithPhases(tournament.Id, ("Pool", new TimeOnly(9, 0)));
        SetupTournamentAndStructure(tournament, structure);

        var volunteer = Volunteer.Create("John", tournament.Id);
        volunteer.AssignShift("Set-up", null);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByIdAsync(volunteer.Id, tournament.Id))
            .Returns(volunteer);
        A.CallTo(() => _factory.FakeVolunteerRepository.UpdateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/volunteers/shifts/Set-up/assign/{volunteer.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
