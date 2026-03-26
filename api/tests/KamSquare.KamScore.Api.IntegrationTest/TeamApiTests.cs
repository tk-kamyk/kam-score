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

public class TeamApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public TeamApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeTeamRepository);
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
    public async Task CreateTeam_Authenticated_ShouldReturnCreated()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("alice");
        A.CallTo(() => _factory.FakeTeamRepository.CreateAsync(A<Team>.Ignored))
            .ReturnsLazily((Team t) => Task.FromResult(t));

        var dto = new TeamDto(null, "Eagles", 75, "eagles@example.com", "+123456789");
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/teams", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TeamDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Eagles");
        result.Level.Should().Be(75);
        result.Email.Should().Be("eagles@example.com");
        result.Phone.Should().Be("+123456789");
    }

    [Fact]
    public async Task UpdateTeam_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var team = Team.Create("Eagles", 75, tournament.Id, "eagles@example.com", "+123456789");
        A.CallTo(() => _factory.FakeTeamRepository.GetByIdAsync(team.Id, tournament.Id))
            .Returns(team);
        A.CallTo(() => _factory.FakeTeamRepository.UpdateAsync(A<Team>.Ignored))
            .ReturnsLazily((Team t) => Task.FromResult(t));
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new TeamDto(null, "Hawks", 80, "hawks@example.com", "+987654321");
        var response = await client.PutAsJsonAsync($"/api/tournaments/{tournament.Id}/teams/{team.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TeamDto>();
        result!.Name.Should().Be("Hawks");
        result.Level.Should().Be(80);
    }

    [Fact]
    public async Task DeleteTeam_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var team = Team.Create("Eagles", 75, tournament.Id);
        A.CallTo(() => _factory.FakeTeamRepository.GetByIdAsync(team.Id, tournament.Id))
            .Returns(team);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}/teams/{team.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        A.CallTo(() => _factory.FakeTeamRepository.DeleteAsync(team.Id, tournament.Id))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CreateTeam_DuplicateName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeTeamRepository.ExistsByNameAsync(tournament.Id, "Eagles", null))
            .Returns(true);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new TeamDto(null, "Eagles", 50, null, null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/teams", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTeam_DuplicateName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var team2 = Team.Create("Hawks", 60, tournament.Id);
        A.CallTo(() => _factory.FakeTeamRepository.GetByIdAsync(team2.Id, tournament.Id))
            .Returns(team2);
        A.CallTo(() => _factory.FakeTeamRepository.ExistsByNameAsync(tournament.Id, "Eagles", team2.Id))
            .Returns(true);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new TeamDto(null, "Eagles", 60, null, null);
        var response = await client.PutAsJsonAsync($"/api/tournaments/{tournament.Id}/teams/{team2.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTeam_LevelOutOfRange_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new TeamDto(null, "Eagles", 101, null, null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/teams", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTeam_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("bob");

        var dto = new TeamDto(null, "Eagles", 75, null, null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/teams", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTeams_Anonymous_ShouldReturnTeamsWithoutContactInfo()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var team = Team.Create("Eagles", 75, tournament.Id, "eagles@example.com", "+123456789");
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { team });
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/teams");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        result.Should().HaveCount(1);
        result![0].Name.Should().Be("Eagles");
        result[0].Email.Should().BeNull();
        result[0].Phone.Should().BeNull();
    }

    [Fact]
    public async Task GetTeams_Owner_ShouldReturnTeamsWithContactInfo()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var team = Team.Create("Eagles", 75, tournament.Id, "eagles@example.com", "+123456789");
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { team });
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/teams");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        result.Should().HaveCount(1);
        result![0].Email.Should().Be("eagles@example.com");
        result![0].Phone.Should().Be("+123456789");
    }

    [Fact]
    public async Task GetTeams_NonOwner_ShouldReturnTeamsWithoutContactInfo()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var team = Team.Create("Eagles", 75, tournament.Id, "eagles@example.com", "+123456789");
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { team });
        var client = _factory.CreateAuthenticatedClient("bob");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/teams");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        result.Should().HaveCount(1);
        result![0].Email.Should().BeNull();
        result![0].Phone.Should().BeNull();
    }

    [Fact]
    public async Task GetTeams_Admin_ShouldReturnTeamsWithContactInfo()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var team = Team.Create("Eagles", 75, tournament.Id, "eagles@example.com", "+123456789");
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { team });
        var client = _factory.CreateAdminClient("admin-user");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/teams");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        result.Should().HaveCount(1);
        result![0].Email.Should().Be("eagles@example.com");
        result![0].Phone.Should().Be("+123456789");
    }

    [Fact]
    public async Task CreateTeam_Admin_ShouldSucceedForOtherUsersTournament()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeTeamRepository.CreateAsync(A<Team>.Ignored))
            .ReturnsLazily((Team t) => Task.FromResult(t));
        var client = _factory.CreateAdminClient("admin-user");

        var dto = new TeamDto(null, "Eagles", 75, null, null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/teams", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTeam_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var dto = new TeamDto(null, "Eagles", 75, null, null);
        var response = await client.PostAsJsonAsync("/api/tournaments/some-id/teams", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTeam_ValidationErrors_ShouldReturnFieldLevelErrors()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new TeamDto(null, "", 101, "not-an-email", null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/teams", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.GetProperty("title").GetString().Should().Be("Validation Error");
        root.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Object);
        root.GetProperty("errors").GetProperty("Name").GetArrayLength().Should().BeGreaterThan(0);
        root.GetProperty("errors").GetProperty("Level").GetArrayLength().Should().BeGreaterThan(0);
        root.GetProperty("errors").GetProperty("Email").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateTeam_DuplicateName_ShouldReturnFieldErrorForName()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeTeamRepository.ExistsByNameAsync(tournament.Id, "Eagles", null))
            .Returns(true);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new TeamDto(null, "Eagles", 50, null, null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/teams", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        doc.RootElement.GetProperty("errors").GetProperty("Name").GetArrayLength().Should().Be(1);
    }

    // --- Generate Seed Teams ---

    [Fact]
    public async Task GenerateSeedTeams_Owner_ShouldReturnCreatedTeams()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeTeamRepository.CountByTournamentIdAsync(tournament.Id))
            .Returns(0);
        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(A<IEnumerable<Team>>.Ignored))
            .ReturnsLazily((IEnumerable<Team> teams) => Task.FromResult(teams));
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/teams/generate",
            new { Count = 4 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        result.Should().HaveCount(4);
        result![0].Name.Should().Be("Seed 1");
        result[1].Name.Should().Be("Seed 2");
        result[2].Name.Should().Be("Seed 3");
        result[3].Name.Should().Be("Seed 4");
        result[0].Level.Should().Be(100);
        result[3].Level.Should().Be(0);
    }

    [Fact]
    public async Task GenerateSeedTeams_Additive_ShouldNumberFromExistingCount()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeTeamRepository.CountByTournamentIdAsync(tournament.Id))
            .Returns(2);
        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(A<IEnumerable<Team>>.Ignored))
            .ReturnsLazily((IEnumerable<Team> teams) => Task.FromResult(teams));
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/teams/generate",
            new { Count = 3 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        result.Should().HaveCount(3);
        result![0].Name.Should().Be("Seed 3");
        result[1].Name.Should().Be("Seed 4");
        result[2].Name.Should().Be("Seed 5");
    }

    [Fact]
    public async Task GenerateSeedTeams_CountZero_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/teams/generate",
            new { Count = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateSeedTeams_CountOver100_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/teams/generate",
            new { Count = 101 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateSeedTeams_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("bob");

        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/teams/generate",
            new { Count = 4 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateSeedTeams_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/tournaments/some-id/teams/generate",
            new { Count = 4 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateSeedTeams_ShouldCreateRealTeamsNotPlaceholders()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeTeamRepository.CountByTournamentIdAsync(tournament.Id))
            .Returns(0);
        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(A<IEnumerable<Team>>.Ignored))
            .ReturnsLazily((IEnumerable<Team> teams) => Task.FromResult(teams));
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/teams/generate",
            new { Count = 2 });

        var result = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        result.Should().AllSatisfy(t =>
        {
            t.IsPlaceholder.Should().BeFalse();
            t.SourcePhaseId.Should().BeNull();
        });
    }

    [Fact]
    public async Task GenerateSeedTeams_NonExistentTournament_ShouldReturn404()
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync("nonexistent"))
            .Returns((Tournament?)null);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsJsonAsync(
            "/api/tournaments/nonexistent/teams/generate",
            new { Count = 4 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
