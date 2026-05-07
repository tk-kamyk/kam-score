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
        A.CallTo(() => _factory.FakeStructureRepository.CreateAsync(A<TournamentStructure>.Ignored))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));

        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, null, null, null);

        var response = await client.PostAsJsonAsync("/api/tournaments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Summer Cup");
        result.TournamentCode.Should().NotBeNullOrEmpty();
        result.OwnerId.Should().Be("alice");
        A.CallTo(() => _factory.FakeStructureRepository.CreateAsync(
                A<TournamentStructure>.That.Matches(s => s.TournamentId == result!.Id)))
            .MustHaveHappenedOnceExactly();
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
    public async Task GetTournaments_Authenticated_ShouldReturnAllTournaments()
    {
        var client = _factory.CreateAuthenticatedClient("alice");
        var ownTournament = Tournament.Create("Cup A", Discipline.Volleyball, "alice");
        var otherTournament = Tournament.Create("Cup B", Discipline.Volleyball, "bob");
        A.CallTo(() => _factory.FakeRepository.GetAllAsync())
            .Returns(new[] { ownTournament, otherTournament });

        var response = await client.GetAsync("/api/tournaments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TournamentDto>>();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTournaments_Authenticated_ShouldShowCodeOnlyForOwnTournaments()
    {
        var client = _factory.CreateAuthenticatedClient("alice");
        var ownTournament = Tournament.Create("Cup A", Discipline.Volleyball, "alice");
        var otherTournament = Tournament.Create("Cup B", Discipline.Volleyball, "bob");
        A.CallTo(() => _factory.FakeRepository.GetAllAsync())
            .Returns(new[] { ownTournament, otherTournament });

        var response = await client.GetAsync("/api/tournaments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TournamentDto>>();
        var cupA = result!.Single(t => t.Name == "Cup A");
        var cupB = result!.Single(t => t.Name == "Cup B");
        cupA.TournamentCode.Should().NotBeNullOrEmpty();
        cupB.TournamentCode.Should().BeNull();
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
        A.CallTo(() => _factory.FakeStructureRepository.CreateAsync(A<TournamentStructure>.Ignored))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));

        var conditions = new GameConditionsDto(3, [21, 21, 15]);
        var dto = new TournamentDto(null, "Beach Cup", "BeachVolleyball", null, 45, conditions, null, null);

        var response = await client.PostAsJsonAsync("/api/tournaments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.GameConditions.Should().NotBeNull();
        result.GameConditions!.BestOfSets.Should().Be(3);
        A.CallTo(() => _factory.FakeStructureRepository.CreateAsync(
                A<TournamentStructure>.That.Matches(s => s.TournamentId == result.Id)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CreateTournament_StructureCreationFails_ShouldRollbackTournament()
    {
        var client = _factory.CreateAuthenticatedClient("alice");
        A.CallTo(() => _factory.FakeRepository.CreateAsync(A<Tournament>.Ignored))
            .ReturnsLazily((Tournament t) => Task.FromResult(t));
        A.CallTo(() => _factory.FakeStructureRepository.CreateAsync(A<TournamentStructure>.Ignored))
            .Throws(new InvalidOperationException("Cosmos DB error"));

        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, null, null, null);

        var response = await client.PostAsJsonAsync("/api/tournaments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        A.CallTo(() => _factory.FakeRepository.DeleteAsync(A<string>.Ignored, "alice"))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTournament_NonOwner_ShouldExcludeCode()
    {
        var client = _factory.CreateAuthenticatedClient("bob");
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.TournamentCode.Should().BeNull();
    }

    // --- Admin role tests ---

    [Fact]
    public async Task UpdateTournament_Admin_ShouldSucceedForOtherUsersTournament()
    {
        var client = _factory.CreateAdminClient("admin-user");
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
    public async Task DeleteTournament_Admin_ShouldSucceedForOtherUsersTournament()
    {
        var client = _factory.CreateAdminClient("admin-user");
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // --- Copy Structure tests ---

    [Fact]
    public async Task CreateTournament_WithSourceTournamentId_ShouldCopyStructure()
    {
        var client = _factory.CreateAuthenticatedClient("bob");
        var source = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        source.Update("Summer Cup", Discipline.Volleyball, DateTime.Parse("2026-06-01"), 60, null);
        var structure = TournamentStructure.Create(source.Id);
        structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 2,
            groupWinners: 2, startTime: new TimeOnly(9, 0));
        var teams = Team.GenerateSeedTeams(8, 1, source.Id);
        var courts = new List<Court> { Court.Create("Main", source.Id), Court.Create("Side", source.Id) };

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(source.Id))
            .Returns(source);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(source.Id))
            .Returns(structure);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(source.Id))
            .Returns(teams);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(source.Id))
            .Returns(courts);
        A.CallTo(() => _factory.FakeRepository.CreateAsync(A<Tournament>.Ignored))
            .ReturnsLazily((Tournament t) => Task.FromResult(t));
        A.CallTo(() => _factory.FakeStructureRepository.CreateAsync(A<TournamentStructure>.Ignored))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));
        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(A<IEnumerable<Team>>.Ignored))
            .ReturnsLazily((IEnumerable<Team> t) => Task.FromResult(t));
        A.CallTo(() => _factory.FakeCourtRepository.CreateBatchAsync(A<IEnumerable<Court>>.Ignored))
            .ReturnsLazily((IEnumerable<Court> c) => Task.FromResult(c));
        A.CallTo(() => _factory.FakeGameRepository.CreateBatchAsync(A<IEnumerable<Game>>.Ignored))
            .ReturnsLazily((IEnumerable<Game> g) => Task.FromResult(g));

        var dto = new TournamentDto(null, "Winter Cup", "Volleyball", null, null, null, null, null,
            SourceTournamentId: source.Id);

        var response = await client.PostAsJsonAsync("/api/tournaments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Winter Cup");
        result.Discipline.Should().Be("Volleyball");
        result.GameLength.Should().Be(60);
        result.OwnerId.Should().Be("bob");
    }

    [Fact]
    public async Task CreateTournament_WithNonExistentSource_ShouldReturn404()
    {
        var client = _factory.CreateAuthenticatedClient("bob");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync("nonexistent"))
            .Returns((Tournament?)null);

        var dto = new TournamentDto(null, "Copy", "Volleyball", null, null, null, null, null,
            SourceTournamentId: "nonexistent");

        var response = await client.PostAsJsonAsync("/api/tournaments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTournament_WithSourceFromAnotherUser_ShouldSucceed()
    {
        var client = _factory.CreateAuthenticatedClient("bob");
        var source = Tournament.Create("Alice Cup", Discipline.BeachVolleyball, "alice");
        source.Update("Alice Cup", Discipline.BeachVolleyball, null, 45, null);
        var structure = TournamentStructure.Create(source.Id);
        structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 1,
            startTime: new TimeOnly(10, 0));
        var teams = Team.GenerateSeedTeams(4, 1, source.Id);
        var courts = new List<Court> { Court.Create("C1", source.Id) };

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(source.Id))
            .Returns(source);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(source.Id))
            .Returns(structure);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(source.Id))
            .Returns(teams);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(source.Id))
            .Returns(courts);
        A.CallTo(() => _factory.FakeRepository.CreateAsync(A<Tournament>.Ignored))
            .ReturnsLazily((Tournament t) => Task.FromResult(t));
        A.CallTo(() => _factory.FakeStructureRepository.CreateAsync(A<TournamentStructure>.Ignored))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));
        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(A<IEnumerable<Team>>.Ignored))
            .ReturnsLazily((IEnumerable<Team> t) => Task.FromResult(t));
        A.CallTo(() => _factory.FakeCourtRepository.CreateBatchAsync(A<IEnumerable<Court>>.Ignored))
            .ReturnsLazily((IEnumerable<Court> c) => Task.FromResult(c));
        A.CallTo(() => _factory.FakeGameRepository.CreateBatchAsync(A<IEnumerable<Game>>.Ignored))
            .ReturnsLazily((IEnumerable<Game> g) => Task.FromResult(g));

        var dto = new TournamentDto(null, "Bob Copy", "Volleyball", null, null, null, null, null,
            SourceTournamentId: source.Id);

        var response = await client.PostAsJsonAsync("/api/tournaments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.OwnerId.Should().Be("bob");
        result.Discipline.Should().Be("BeachVolleyball");
    }

    [Fact]
    public async Task GetTournaments_Admin_ShouldShowCodesForAllTournaments()
    {
        var client = _factory.CreateAdminClient("admin-user");
        var aliceTournament = Tournament.Create("Cup A", Discipline.Volleyball, "alice");
        var bobTournament = Tournament.Create("Cup B", Discipline.Volleyball, "bob");
        A.CallTo(() => _factory.FakeRepository.GetAllAsync())
            .Returns(new[] { aliceTournament, bobTournament });

        var response = await client.GetAsync("/api/tournaments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TournamentDto>>();
        result!.Should().HaveCount(2);
        result[0].TournamentCode.Should().NotBeNullOrEmpty();
        result[1].TournamentCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTournament_Admin_ShouldShowCodeForOtherUsersTournament()
    {
        var client = _factory.CreateAdminClient("admin-user");
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.TournamentCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTournaments_ShouldIncludeOwnerDisplayName()
    {
        var client = _factory.CreateClient();
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "admin");
        A.CallTo(() => _factory.FakeRepository.GetAllAsync())
            .Returns(new[] { tournament });

        var response = await client.GetAsync("/api/tournaments");

        var result = await response.Content.ReadFromJsonAsync<List<TournamentDto>>();
        result![0].OwnerDisplayName.Should().Be("Administrator");
    }

    [Fact]
    public async Task GetTournament_ShouldIncludeOwnerDisplayName()
    {
        var client = _factory.CreateClient();
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "admin");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}");

        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.OwnerDisplayName.Should().Be("Administrator");
    }

    [Fact]
    public async Task GetTournament_UnknownOwner_ShouldFallBackToOwnerId()
    {
        var client = _factory.CreateClient();
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}");

        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.OwnerDisplayName.Should().Be("alice");
    }

    [Fact]
    public async Task GetTournaments_UnknownOwner_ShouldFallBackToOwnerId()
    {
        var client = _factory.CreateClient();
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetAllAsync())
            .Returns(new[] { tournament });

        var response = await client.GetAsync("/api/tournaments");

        var result = await response.Content.ReadFromJsonAsync<List<TournamentDto>>();
        result![0].OwnerDisplayName.Should().Be("alice");
    }

    [Fact]
    public async Task UpdateTournament_ShouldEnrichResponseWithOwnerDisplayNameAndCounts()
    {
        var client = _factory.CreateAuthenticatedClient("admin");
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "admin");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        A.CallTo(() => _factory.FakeRepository.UpdateAsync(A<Tournament>.Ignored))
            .ReturnsLazily((Tournament t) => Task.FromResult(t));
        A.CallTo(() => _factory.FakeTeamRepository.CountByTournamentIdAsync(tournament.Id))
            .Returns(7);
        A.CallTo(() => _factory.FakeCourtRepository.CountByTournamentIdAsync(tournament.Id))
            .Returns(3);

        var dto = new TournamentDto(null, "Winter Cup", "Volleyball", null, 30, null, null, null);

        var response = await client.PutAsJsonAsync($"/api/tournaments/{tournament.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.OwnerDisplayName.Should().Be("Administrator");
        result.TeamCount.Should().Be(7);
        result.CourtCount.Should().Be(3);
    }

    [Fact]
    public async Task CreateTournament_ShouldEnrichResponseWithOwnerDisplayNameAndCounts()
    {
        var client = _factory.CreateAuthenticatedClient("admin");
        A.CallTo(() => _factory.FakeRepository.CreateAsync(A<Tournament>.Ignored))
            .ReturnsLazily((Tournament t) => Task.FromResult(t));
        A.CallTo(() => _factory.FakeStructureRepository.CreateAsync(A<TournamentStructure>.Ignored))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));
        A.CallTo(() => _factory.FakeTeamRepository.CountByTournamentIdAsync(A<string>.Ignored))
            .Returns(2);
        A.CallTo(() => _factory.FakeCourtRepository.CountByTournamentIdAsync(A<string>.Ignored))
            .Returns(1);

        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, null, null, null);

        var response = await client.PostAsJsonAsync("/api/tournaments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.OwnerDisplayName.Should().Be("Administrator");
        result.TeamCount.Should().Be(2);
        result.CourtCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateTournament_FromSource_ShouldEnrichResponseWithOwnerDisplayNameAndCounts()
    {
        var client = _factory.CreateAuthenticatedClient("admin");
        var source = Tournament.Create("Source Cup", Discipline.Volleyball, "admin");
        var structure = TournamentStructure.Create(source.Id);
        var teams = Team.GenerateSeedTeams(4, 1, source.Id);
        var courts = new List<Court> { Court.Create("Main", source.Id) };

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(source.Id))
            .Returns(source);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(source.Id))
            .Returns(structure);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(source.Id))
            .Returns(teams);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(source.Id))
            .Returns(courts);
        A.CallTo(() => _factory.FakeRepository.CreateAsync(A<Tournament>.Ignored))
            .ReturnsLazily((Tournament t) => Task.FromResult(t));
        A.CallTo(() => _factory.FakeStructureRepository.CreateAsync(A<TournamentStructure>.Ignored))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));
        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(A<IEnumerable<Team>>.Ignored))
            .ReturnsLazily((IEnumerable<Team> t) => Task.FromResult(t));
        A.CallTo(() => _factory.FakeCourtRepository.CreateBatchAsync(A<IEnumerable<Court>>.Ignored))
            .ReturnsLazily((IEnumerable<Court> c) => Task.FromResult(c));
        A.CallTo(() => _factory.FakeGameRepository.CreateBatchAsync(A<IEnumerable<Game>>.Ignored))
            .ReturnsLazily((IEnumerable<Game> g) => Task.FromResult(g));
        A.CallTo(() => _factory.FakeTeamRepository.CountByTournamentIdAsync(A<string>.Ignored))
            .Returns(4);
        A.CallTo(() => _factory.FakeCourtRepository.CountByTournamentIdAsync(A<string>.Ignored))
            .Returns(1);

        var dto = new TournamentDto(null, "Copy Cup", "Volleyball", null, null, null, null, null,
            SourceTournamentId: source.Id);

        var response = await client.PostAsJsonAsync("/api/tournaments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.OwnerDisplayName.Should().Be("Administrator");
        result.TeamCount.Should().Be(4);
        result.CourtCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteTournament_Owner_ShouldAlsoDeleteAllRelatedEntities()
    {
        var client = _factory.CreateAuthenticatedClient("alice");
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        A.CallTo(() => _factory.FakeGameRepository.DeleteByTournamentIdAsync(tournament.Id))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _factory.FakeTeamRepository.DeleteByTournamentIdAsync(tournament.Id))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _factory.FakeCourtRepository.DeleteByTournamentIdAsync(tournament.Id))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _factory.FakeStructureRepository.DeleteByTournamentIdAsync(tournament.Id))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _factory.FakeRepository.DeleteAsync(tournament.Id, "alice"))
            .MustHaveHappenedOnceExactly();
    }
}
