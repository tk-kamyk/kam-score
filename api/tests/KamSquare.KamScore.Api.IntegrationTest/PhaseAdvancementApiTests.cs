using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class PhaseAdvancementApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public PhaseAdvancementApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        Fake.Reset(factory.FakeRepository);
        Fake.Reset(factory.FakeStructureRepository);
        Fake.Reset(factory.FakeTeamRepository);
        Fake.Reset(factory.FakeCourtRepository);
        Fake.Reset(factory.FakeGameRepository);
    }

    private Tournament CreateTestTournament(string ownerId = "alice")
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, ownerId);
        tournament.Update("Summer Cup", Discipline.Volleyball,
            new DateTime(2026, 6, 1), 30, null);
        return tournament;
    }

    private TournamentStructure CreateTwoPhaseStructure(string tournamentId)
    {
        var structure = TournamentStructure.Create(tournamentId);
        var phase1 = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 2,
            groupWinners: 2, totalTeamsProceeding: 4, startTime: new TimeOnly(9, 0));
        var phase2 = structure.AddPhase("Playoffs", PhaseFormat.PlayoffElimination, 1,
            startTime: new TimeOnly(14, 0));

        // Assign 4 teams to each group in phase 1
        for (var i = 0; i < 4; i++)
            phase1.Groups[0].AddTeam($"team{i + 1}");
        for (var i = 4; i < 8; i++)
            phase1.Groups[1].AddTeam($"team{i + 1}");

        // Activate phase 1
        phase1.Activate();

        return structure;
    }

    private List<Game> CreateCompletedGamesForPhase(
        string tournamentId, Phase phase, List<(int home, int away)>? scores = null)
    {
        var games = new List<Game>();
        var gameIndex = 0;
        foreach (var group in phase.Groups)
        {
            for (var i = 0; i < group.TeamIds.Count; i++)
            {
                for (var j = i + 1; j < group.TeamIds.Count; j++)
                {
                    var game = Game.Create(tournamentId, phase.Id, group.Id, 1,
                        homeTeamId: group.TeamIds[i], awayTeamId: group.TeamIds[j]);
                    if (scores is not null && gameIndex < scores.Count)
                        game.RecordSimpleResult(scores[gameIndex].home, scores[gameIndex].away);
                    else
                        game.RecordSimpleResult(2, 1); // default: home wins
                    games.Add(game);
                    gameIndex++;
                }
            }
        }
        return games;
    }

    private void SetupFakes(Tournament tournament, TournamentStructure structure)
    {
        A.CallTo(() => _factory.FakeRepository.GetByIdAsync(tournament.Id))
            .Returns(tournament);
        A.CallTo(() => _factory.FakeStructureRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(structure);
        A.CallTo(() => _factory.FakeStructureRepository.UpdateAsync(A<TournamentStructure>.Ignored))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));
        A.CallTo(() => _factory.FakeGameRepository.UpdateAsync(A<Game>.Ignored))
            .ReturnsLazily((Game g) => Task.FromResult(g));
    }

    // --- Complete Phase ---

    [Fact]
    public async Task CompletePhase_HappyPath_ReturnsOkWithCompletedStatus()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        var games = CreateCompletedGamesForPhase(tournament.Id, phase1);

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(games);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, structure.Phases[1].Id))
            .Returns(new List<Game>());

        var client = _factory.CreateAuthenticatedClient("alice");
        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task CompletePhase_AssignsTeamsToNextPhaseAndActivatesIt()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        var phase2 = structure.Phases[1];
        var games = CreateCompletedGamesForPhase(tournament.Id, phase1);

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(games);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase2.Id))
            .Returns(new List<Game>());

        var client = _factory.CreateAuthenticatedClient("alice");
        await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/complete", null);

        // Next phase should be activated with teams assigned
        phase2.Status.Should().Be(PhaseStatus.InProgress);
        phase2.Groups.SelectMany(g => g.TeamIds).Should().HaveCount(4);
    }

    [Fact]
    public async Task CompletePhase_ResolvesPlaceholdersInNextPhaseGames()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        var phase2 = structure.Phases[1];
        var games = CreateCompletedGamesForPhase(tournament.Id, phase1);

        // Create placeholder games for phase 2
        var placeholderGames = new List<Game>
        {
            Game.Create(tournament.Id, phase2.Id, phase2.Groups[0].Id, 1,
                homeTeamPlaceholder: "Group Stage - Seed 1",
                awayTeamPlaceholder: "Group Stage - Seed 4"),
            Game.Create(tournament.Id, phase2.Id, phase2.Groups[0].Id, 1,
                homeTeamPlaceholder: "Group Stage - Seed 2",
                awayTeamPlaceholder: "Group Stage - Seed 3")
        };

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(games);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase2.Id))
            .Returns(placeholderGames);

        var client = _factory.CreateAuthenticatedClient("alice");
        await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/complete", null);

        // Placeholder games should now have real team IDs
        placeholderGames.Should().OnlyContain(g =>
            g.HomeTeamId != null && g.AwayTeamId != null);
        // Placeholder strings should be preserved
        placeholderGames[0].HomeTeamPlaceholder.Should().Be("Group Stage - Seed 1");
    }

    [Fact]
    public async Task CompletePhase_FailsIfNotInProgress()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        phase1.Status = PhaseStatus.New; // Override to New

        SetupFakes(tournament, structure);

        var client = _factory.CreateAuthenticatedClient("alice");
        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompletePhase_FailsIfGamesNotCompleted()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        var games = new List<Game>
        {
            Game.Create(tournament.Id, phase1.Id, phase1.Groups[0].Id, 1,
                homeTeamId: "team1", awayTeamId: "team2") // Status = Scheduled (not completed)
        };

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(games);

        var client = _factory.CreateAuthenticatedClient("alice");
        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompletePhase_NonOwner_Returns403()
    {
        var tournament = CreateTestTournament("alice");
        var structure = CreateTwoPhaseStructure(tournament.Id);
        SetupFakes(tournament, structure);

        var client = _factory.CreateAuthenticatedClient("bob");
        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{structure.Phases[0].Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Reopen Phase ---

    [Fact]
    public async Task ReopenPhase_RevertsToInProgress()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        phase1.Complete();

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(
            tournament.Id, structure.Phases[1].Id)).Returns(new List<Game>());

        var client = _factory.CreateAuthenticatedClient("alice");
        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/reopen", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PhaseDto>();
        result!.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task ReopenPhase_ClearsNextPhaseTeamsAndRevertsToNew()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        var phase2 = structure.Phases[1];
        phase1.Complete();
        structure.AutoAssignTeams(phase2.Id, ["t1", "t2", "t3", "t4"]);
        phase2.Activate();

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(
            tournament.Id, phase2.Id)).Returns(new List<Game>());

        var client = _factory.CreateAuthenticatedClient("alice");
        await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/reopen", null);

        phase2.Status.Should().Be(PhaseStatus.New);
        phase2.Groups.Should().AllSatisfy(g => g.TeamIds.Should().BeEmpty());
    }

    [Fact]
    public async Task ReopenPhase_UnresolvesPlaceholdersInNextPhaseGames()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        var phase2 = structure.Phases[1];
        phase1.Complete();

        var resolvedGames = new List<Game>
        {
            Game.Create(tournament.Id, phase2.Id, phase2.Groups[0].Id, 1,
                homeTeamId: "team1", awayTeamId: "team4",
                homeTeamPlaceholder: "Group Stage - Seed 1",
                awayTeamPlaceholder: "Group Stage - Seed 4")
        };

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(
            tournament.Id, phase2.Id)).Returns(resolvedGames);

        var client = _factory.CreateAuthenticatedClient("alice");
        await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/reopen", null);

        resolvedGames[0].HomeTeamId.Should().BeNull();
        resolvedGames[0].AwayTeamId.Should().BeNull();
        resolvedGames[0].HomeTeamPlaceholder.Should().Be("Group Stage - Seed 1");
    }

    [Fact]
    public async Task ReopenPhase_FailsIfNotCompleted()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        // phase1 is InProgress, not Completed

        SetupFakes(tournament, structure);

        var client = _factory.CreateAuthenticatedClient("alice");
        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{structure.Phases[0].Id}/reopen", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReopenPhase_NonOwner_Returns403()
    {
        var tournament = CreateTestTournament("alice");
        var structure = CreateTwoPhaseStructure(tournament.Id);
        structure.Phases[0].Complete();
        SetupFakes(tournament, structure);

        var client = _factory.CreateAuthenticatedClient("bob");
        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{structure.Phases[0].Id}/reopen", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Game Generation with Placeholders ---

    [Fact]
    public async Task GenerateGames_Phase2WithPlaceholders_ReturnsCreated()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase2 = structure.Phases[1];
        var courts = new List<Court> { Court.Create("Court 1", tournament.Id) };

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(courts);
        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(
            tournament.Id, phase2.Id)).Returns(false);
        A.CallTo(() => _factory.FakeGameRepository.CreateBatchAsync(A<IEnumerable<Game>>.Ignored))
            .ReturnsLazily((IEnumerable<Game> games) => Task.FromResult(games));
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new List<Team>());

        var client = _factory.CreateAuthenticatedClient("alice");
        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase2.Id}/generate-schedule", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var games = await response.Content.ReadFromJsonAsync<List<GameDto>>();
        games.Should().NotBeNull();
        games!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateGames_Phase1_ActivatesPhase()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 1,
            startTime: new TimeOnly(9, 0));
        phase.Groups[0].AddTeam("team1");
        phase.Groups[0].AddTeam("team2");
        phase.Groups[0].AddTeam("team3");

        var courts = new List<Court> { Court.Create("Court 1", tournament.Id) };
        var teams = new List<Team>
        {
            Team.Create("Team 1", 50, tournament.Id),
            Team.Create("Team 2", 40, tournament.Id),
            Team.Create("Team 3", 30, tournament.Id)
        };

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(courts);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(teams);
        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(
            tournament.Id, phase.Id)).Returns(false);
        A.CallTo(() => _factory.FakeGameRepository.CreateBatchAsync(A<IEnumerable<Game>>.Ignored))
            .ReturnsLazily((IEnumerable<Game> games) => Task.FromResult(games));

        var client = _factory.CreateAuthenticatedClient("alice");
        await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase.Id}/generate-schedule", null);

        phase.Status.Should().Be(PhaseStatus.InProgress);
    }
}
