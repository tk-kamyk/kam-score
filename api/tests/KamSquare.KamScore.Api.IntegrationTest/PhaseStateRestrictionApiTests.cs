using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Api.Endpoints;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class PhaseStateRestrictionApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public PhaseStateRestrictionApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeStructureRepository);
        Fake.Reset(factory.FakeGameRepository);
        Fake.Reset(factory.FakeTeamRepository);
        Fake.Reset(factory.FakeCourtRepository);
    }

    private Tournament CreateTestTournament(string ownerId = "alice")
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, ownerId);
        tournament.Update("Summer Cup", Discipline.Volleyball,
            new DateTime(2026, 6, 1), 30, null);
        return tournament;
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

    // --- 1. UpdatePhase when Completed returns 409 ---

    [Fact]
    public async Task UpdatePhase_WhenCompleted_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        phase.Activate();
        phase.Complete();
        SetupTournamentAndStructure(tournament, structure);

        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new PhaseDto(null, "Renamed Groups", "RoundRobin");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 2. UpdatePhase structural fields with games returns 409 ---

    [Fact]
    public async Task UpdatePhase_StructuralFieldsWithGames_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        phase.Activate();
        SetupTournamentAndStructure(tournament, structure);

        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(tournament.Id, phase.Id))
            .Returns(true);

        var client = _factory.CreateAuthenticatedClient("alice");

        // Change format from RoundRobin to PlayoffElimination (structural change)
        var dto = new PhaseDto(null, "Groups", "PlayoffElimination");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 3. UpdatePhase name-only change with games returns 200 ---

    [Fact]
    public async Task UpdatePhase_NameChangeWithGames_Returns200()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        phase.Activate();
        SetupTournamentAndStructure(tournament, structure);

        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(tournament.Id, phase.Id))
            .Returns(true);

        var client = _factory.CreateAuthenticatedClient("alice");

        // Only change name, keep same format — not a structural change
        var dto = new PhaseDto(null, "Renamed Groups", "RoundRobin");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.Name.Should().Be("Renamed Groups");
    }

    // --- 3a. UpdatePhase progression fields with games returns 200 ---

    [Fact]
    public async Task UpdatePhase_ProgressionFieldsWithGames_Returns200()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        phase.Activate();
        SetupTournamentAndStructure(tournament, structure);

        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(tournament.Id, phase.Id))
            .Returns(true);

        var client = _factory.CreateAuthenticatedClient("alice");

        // Change groupWinners (default: null) and totalTeamsProceeding (default: null) — not structural
        var dto = new PhaseDto(null, "Groups", "RoundRobin", GroupWinners: 2, TotalTeamsProceeding: 6);
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.GroupWinners.Should().Be(2);
        result!.TotalTeamsProceeding.Should().Be(6);
    }

    // --- 3b. UpdatePhase start time with games returns 409 ---

    [Fact]
    public async Task UpdatePhase_StartTimeWithGames_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2,
            startTime: new TimeOnly(9, 0));
        phase.Activate();
        SetupTournamentAndStructure(tournament, structure);

        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(tournament.Id, phase.Id))
            .Returns(true);

        var client = _factory.CreateAuthenticatedClient("alice");

        // Change start time — structural change
        var dto = new PhaseDto(null, "Groups", "RoundRobin", StartTime: "14:00");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 4. DeletePhase when Completed returns 409 ---

    [Fact]
    public async Task DeletePhase_WhenCompleted_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        phase.Activate();
        phase.Complete();
        SetupTournamentAndStructure(tournament, structure);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 5. DeletePhase when games exist returns 409 ---

    [Fact]
    public async Task DeletePhase_WhenGamesExist_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        phase.Activate();
        SetupTournamentAndStructure(tournament, structure);

        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(tournament.Id, phase.Id))
            .Returns(true);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 6. AutoAssignTeams when games exist returns 409 ---

    [Fact]
    public async Task AutoAssignTeams_WhenGamesExist_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        phase.Activate();
        SetupTournamentAndStructure(tournament, structure);

        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(tournament.Id, phase.Id))
            .Returns(true);

        var teams = new[]
        {
            Team.Create("Eagles", 90, tournament.Id),
            Team.Create("Hawks", 80, tournament.Id),
        };
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(teams);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/auto-assign", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 7. AddGroup when Completed returns 409 ---

    [Fact]
    public async Task AddGroup_WhenCompleted_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        phase.Activate();
        phase.Complete();
        SetupTournamentAndStructure(tournament, structure);

        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new GroupDto(null, "D");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 8. AddGroup when games exist returns 409 ---

    [Fact]
    public async Task AddGroup_WhenGamesExist_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        phase.Activate();
        SetupTournamentAndStructure(tournament, structure);

        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(tournament.Id, phase.Id))
            .Returns(true);

        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new GroupDto(null, "D");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 9. UpdateGroupName when Completed returns 409 ---

    [Fact]
    public async Task UpdateGroupName_WhenCompleted_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var groupId = phase.Groups[0].Id;
        phase.Activate();
        phase.Complete();
        SetupTournamentAndStructure(tournament, structure);

        var client = _factory.CreateAuthenticatedClient("alice");

        var dto = new GroupDto(null, "Pool 1");
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 10. DeleteGroup when games exist returns 409 ---

    [Fact]
    public async Task DeleteGroup_WhenGamesExist_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var groupId = phase.Groups[0].Id;
        phase.Activate();
        SetupTournamentAndStructure(tournament, structure);

        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(tournament.Id, phase.Id))
            .Returns(true);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 11. AssignTeam when games exist returns 409 ---

    [Fact]
    public async Task AssignTeam_WhenGamesExist_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 2);
        var groupId = phase.Groups[0].Id;
        phase.Activate();
        SetupTournamentAndStructure(tournament, structure);

        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(tournament.Id, phase.Id))
            .Returns(true);

        var team = Team.Create("Eagles", 50, tournament.Id);
        A.CallTo(() => _factory.FakeTeamRepository.GetByIdAsync(team.Id, tournament.Id))
            .Returns(team);

        var client = _factory.CreateAuthenticatedClient("alice");

        var request = new TeamAssignmentRequest(team.Id);
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}/teams",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 12. RemoveTeam when games exist returns 409 ---

    [Fact]
    public async Task RemoveTeam_WhenGamesExist_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        var groupId = phase.Groups[0].Id;
        var team = Team.Create("Eagles", 50, tournament.Id);
        structure.AssignTeam(phase.Id, groupId, team.Id);
        phase.Activate();
        SetupTournamentAndStructure(tournament, structure);

        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(tournament.Id, phase.Id))
            .Returns(true);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/groups/{groupId}/teams/{team.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 13. DeleteGames when Completed returns 409 ---

    [Fact]
    public async Task DeleteGames_WhenCompleted_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        phase.Activate();
        phase.Complete();
        SetupTournamentAndStructure(tournament, structure);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/games");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 14. DeleteGames when InProgress resets phase to New ---

    [Fact]
    public async Task DeleteGames_WhenInProgress_ResetsPhaseToNew()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        phase.Activate();
        SetupTournamentAndStructure(tournament, structure);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/games");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        A.CallTo(() => _factory.FakeGameRepository.DeleteByPhaseIdAsync(tournament.Id, phase.Id))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _factory.FakeStructureRepository.UpdateAsync(
            A<TournamentStructure>.That.Matches(s =>
                s.Phases.First().Status == PhaseStatus.New)))
            .MustHaveHappenedOnceExactly();
    }

    // --- 15. RecordResult when phase completed returns 409 ---

    [Fact]
    public async Task RecordResult_WhenPhaseCompleted_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        phase.Activate();
        phase.Complete();

        var game = Game.Create(tournament.Id, phase.Id, phase.Groups[0].Id, 1,
            homeTeamId: "team1", awayTeamId: "team2");
        game.AssignSchedule("court1", new DateTime(2026, 6, 1, 9, 0, 0));

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeGameRepository.GetByIdAsync(tournament.Id, game.Id)).Returns(game);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);

        var client = _factory.CreateAuthenticatedClient("alice");

        var result = new GameResultDto(Sets: null, HomeScore: 2, AwayScore: 1);
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{game.Id}/result", result);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 16. RecordResult when team unassigned returns 400 ---

    [Fact]
    public async Task RecordResult_WhenTeamUnassigned_Returns400()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Playoffs", PhaseFormat.PlayoffElimination, 1);
        phase.Activate();

        // Create a game with placeholder teams (no real teams assigned yet)
        var game = Game.Create(tournament.Id, phase.Id, phase.Groups[0].Id, 1,
            homeTeamId: null, awayTeamId: null,
            homeTeamPlaceholder: "Winner SF1", awayTeamPlaceholder: "Winner SF2");
        game.AssignSchedule("court1", new DateTime(2026, 6, 1, 9, 0, 0));

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeGameRepository.GetByIdAsync(tournament.Id, game.Id)).Returns(game);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);

        var client = _factory.CreateAuthenticatedClient("alice");

        var result = new GameResultDto(Sets: null, HomeScore: 2, AwayScore: 1);
        var response = await client.PutAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/games/{game.Id}/result", result);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- 17. ReopenPhase when next phase has completed games returns 409 ---

    [Fact]
    public async Task ReopenPhase_WhenNextPhaseHasCompletedGames_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase1 = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 2,
            groupWinners: 2, totalTeamsProceeding: 4);
        var phase2 = structure.AddPhase("Playoffs", PhaseFormat.PlayoffElimination, 1);
        phase1.Activate();
        phase1.Complete();
        phase2.Activate();

        // Phase 2 has a completed game
        var completedGame = Game.Create(tournament.Id, phase2.Id, phase2.Groups[0].Id, 1,
            homeTeamId: "team1", awayTeamId: "team2");
        completedGame.RecordSimpleResult(2, 1);

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);
        A.CallTo(() => _factory.FakeStructureRepository.UpdateAsync(A<TournamentStructure>.Ignored))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase2.Id))
            .Returns(new List<Game> { completedGame });
        A.CallTo(() => _factory.FakeTeamRepository.GetBySourcePhaseIdAsync(
            A<string>.Ignored, A<string>.Ignored))
            .Returns(new List<Team>());

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/reopen", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 18. DeleteTeam when referenced in games returns 409 ---

    [Fact]
    public async Task DeleteTeam_WhenReferencedInGames_Returns409()
    {
        var tournament = CreateTestTournament();
        var team = Team.Create("Eagles", 75, tournament.Id);

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeTeamRepository.GetByIdAsync(team.Id, tournament.Id)).Returns(team);
        // Return empty structure (team not in any group) to reach the game-reference check
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(TournamentStructure.Create(tournament.Id));

        A.CallTo(() => _factory.FakeGameRepository.TeamIsReferencedInGamesAsync(tournament.Id, team.Id))
            .Returns(true);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/teams/{team.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 19. DeleteTeam when assigned to group returns 409 ---

    [Fact]
    public async Task DeleteTeam_WhenAssignedToGroup_Returns409()
    {
        var tournament = CreateTestTournament();
        var team = Team.Create("Eagles", 75, tournament.Id);
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1);
        structure.AssignTeam(phase.Id, phase.Groups[0].Id, team.Id);

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeTeamRepository.GetByIdAsync(team.Id, tournament.Id)).Returns(team);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/teams/{team.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- 20. DeleteCourt when referenced in games returns 409 ---

    [Fact]
    public async Task DeleteCourt_WhenReferencedInGames_Returns409()
    {
        var tournament = CreateTestTournament();
        var court = Court.Create("Court 1", tournament.Id);

        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id)).Returns(tournament);
        A.CallTo(() => _factory.FakeCourtRepository.GetByIdAsync(court.Id, tournament.Id)).Returns(court);

        var game = Game.Create(tournament.Id, "p1", "g1", 1,
            homeTeamId: "team1", awayTeamId: "team2");
        game.AssignSchedule(court.Id, new DateTime(2026, 6, 1, 9, 0, 0));
        A.CallTo(() => _factory.FakeGameRepository.GetGamesAsync(
            tournament.Id, null, null, court.Id))
            .Returns(new List<Game> { game });

        var client = _factory.CreateAuthenticatedClient("alice");

        var response = await client.DeleteAsync(
            $"/api/tournaments/{tournament.Id}/courts/{court.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
