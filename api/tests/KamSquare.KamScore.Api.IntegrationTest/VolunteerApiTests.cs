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

public class VolunteerApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public VolunteerApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeVolunteerRepository);
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

    // --- Create ---

    [Fact]
    public async Task CreateVolunteer_Owner_ShouldReturnCreated()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("alice");
        A.CallTo(() => _factory.FakeVolunteerRepository.CreateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));

        var dto = new VolunteerDto(null, "John Doe", "john@email.com", null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/volunteers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<VolunteerDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("John Doe");
        result.Contact.Should().Be("john@email.com");
    }

    [Fact]
    public async Task CreateVolunteer_WithTeamId_ShouldValidateTeamExists()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var team = Team.Create("Eagles", 75, tournament.Id);
        A.CallTo(() => _factory.FakeTeamRepository.GetByIdAsync(team.Id, tournament.Id))
            .Returns(team);
        A.CallTo(() => _factory.FakeVolunteerRepository.CreateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new VolunteerDto(null, "John Doe", null, team.Id);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/volunteers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<VolunteerDto>();
        result!.TeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task CreateVolunteer_WithNonExistentTeam_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeTeamRepository.GetByIdAsync("non-existent", tournament.Id))
            .Returns((Team?)null);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new VolunteerDto(null, "John Doe", null, "non-existent");
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/volunteers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateVolunteer_Admin_ShouldReturnCreated()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeVolunteerRepository.CreateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));
        var client = _factory.CreateAdminClient("admin-user");

        var dto = new VolunteerDto(null, "John Doe", null, null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/volunteers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateVolunteer_DuplicateName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeVolunteerRepository.ExistsByNameAsync(tournament.Id, "John Doe", null))
            .Returns(true);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new VolunteerDto(null, "John Doe", null, null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/volunteers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateVolunteer_EmptyName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new VolunteerDto(null, "", null, null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/volunteers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        doc.RootElement.GetProperty("errors").GetProperty("Name").GetArrayLength().Should().BeGreaterThan(0);
    }

    // --- Read ---

    [Fact]
    public async Task GetVolunteers_Owner_ShouldReturnVolunteers()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var volunteer = Volunteer.Create("John Doe", tournament.Id, "john@email.com", null);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { volunteer });
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/volunteers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<VolunteerDto>>();
        result.Should().HaveCount(1);
        result![0].Name.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetVolunteers_Admin_ShouldReturnVolunteers()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Volunteer>());
        var client = _factory.CreateAdminClient("admin-user");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/volunteers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetVolunteers_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tournaments/some-id/volunteers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetVolunteers_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("bob");

        var response = await client.GetAsync($"/api/tournaments/{tournament.Id}/volunteers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Update ---

    [Fact]
    public async Task UpdateVolunteer_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var volunteer = Volunteer.Create("John Doe", tournament.Id);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByIdAsync(volunteer.Id, tournament.Id))
            .Returns(volunteer);
        A.CallTo(() => _factory.FakeVolunteerRepository.UpdateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new VolunteerDto(null, "Jane Smith", "new@email.com", null);
        var response = await client.PutAsJsonAsync($"/api/tournaments/{tournament.Id}/volunteers/{volunteer.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VolunteerDto>();
        result!.Name.Should().Be("Jane Smith");
        result.Contact.Should().Be("new@email.com");
    }

    [Fact]
    public async Task UpdateVolunteer_DuplicateName_ShouldReturn400()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var volunteer = Volunteer.Create("Jane Doe", tournament.Id);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByIdAsync(volunteer.Id, tournament.Id))
            .Returns(volunteer);
        A.CallTo(() => _factory.FakeVolunteerRepository.ExistsByNameAsync(tournament.Id, "John Doe", volunteer.Id))
            .Returns(true);
        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new VolunteerDto(null, "John Doe", null, null);
        var response = await client.PutAsJsonAsync($"/api/tournaments/{tournament.Id}/volunteers/{volunteer.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- Delete ---

    [Fact]
    public async Task DeleteVolunteer_Owner_ShouldSucceed()
    {
        var tournament = CreateTestTournament();
        SetupTournament(tournament);
        var volunteer = Volunteer.Create("John Doe", tournament.Id);
        A.CallTo(() => _factory.FakeVolunteerRepository.GetByIdAsync(volunteer.Id, tournament.Id))
            .Returns(volunteer);
        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}/volunteers/{volunteer.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        A.CallTo(() => _factory.FakeVolunteerRepository.DeleteAsync(volunteer.Id, tournament.Id))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteVolunteer_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("bob");

        var response = await client.DeleteAsync($"/api/tournaments/{tournament.Id}/volunteers/some-id");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateVolunteer_Anonymous_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var dto = new VolunteerDto(null, "John Doe", null, null);
        var response = await client.PostAsJsonAsync("/api/tournaments/some-id/volunteers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateVolunteer_NonOwner_ShouldReturn403()
    {
        var tournament = CreateTestTournament("alice");
        SetupTournament(tournament);
        var client = _factory.CreateAuthenticatedClient("bob");

        var dto = new VolunteerDto(null, "John Doe", null, null);
        var response = await client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/volunteers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
