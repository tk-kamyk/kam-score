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

    private static List<Team> CreatePlaceholderTeams(string sourcePhaseId, int count)
    {
        return Enumerable.Range(1, count)
            .Select(seed => Team.CreatePlaceholder($"Group Stage - Seed {seed}", "t1", sourcePhaseId, seed))
            .ToList();
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
        A.CallTo(() => _factory.FakeTeamRepository.GetBySourcePhaseIdAsync(
            A<string>.Ignored, A<string>.Ignored))
            .Returns(new List<Team>());
        A.CallTo(() => _factory.FakeTeamRepository.UpdateAsync(A<Team>.Ignored))
            .ReturnsLazily((Team t) => Task.FromResult(t));
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
    public async Task CompletePhase_AssignsTeamsToNextPhaseButKeepsItNew()
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

        // Next phase should have teams assigned but remain New (activated only when games are generated)
        phase2.Status.Should().Be(PhaseStatus.New);
        phase2.Groups.SelectMany(g => g.TeamIds).Should().HaveCount(4);
    }

    [Fact]
    public async Task CompletePhase_ResolvesPlaceholderTeamsInNextPhaseGames()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        var phase2 = structure.Phases[1];
        var games = CreateCompletedGamesForPhase(tournament.Id, phase1);

        // Create placeholder teams for phase 2
        var placeholders = CreatePlaceholderTeams(phase1.Id, 4);

        // Assign placeholder teams to phase 2 group
        foreach (var p in placeholders)
            phase2.Groups[0].AddTeam(p.Id);

        // Create games with placeholder team IDs
        var phase2Games = new List<Game>
        {
            Game.Create(tournament.Id, phase2.Id, phase2.Groups[0].Id, 1,
                homeTeamId: placeholders[0].Id, awayTeamId: placeholders[3].Id),
            Game.Create(tournament.Id, phase2.Id, phase2.Groups[0].Id, 1,
                homeTeamId: placeholders[1].Id, awayTeamId: placeholders[2].Id)
        };

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(games);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase2.Id))
            .Returns(phase2Games);
        A.CallTo(() => _factory.FakeTeamRepository.GetBySourcePhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(placeholders);

        var client = _factory.CreateAuthenticatedClient("alice");
        await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/complete", null);

        // Games should now have real team IDs (not placeholder IDs)
        phase2Games.Should().OnlyContain(g =>
            g.HomeTeamId != null && g.AwayTeamId != null
            && !placeholders.Any(p => p.Id == g.HomeTeamId)
            && !placeholders.Any(p => p.Id == g.AwayTeamId));

        // Placeholder teams should have ResolvedTeamId set
        placeholders.Should().OnlyContain(p => p.ResolvedTeamId != null);
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
    public async Task ReopenPhase_UnresolvesPlaceholderTeamsInNextPhaseGames()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        var phase2 = structure.Phases[1];
        phase1.Complete();

        // Create placeholder teams that were previously resolved
        var placeholders = CreatePlaceholderTeams(phase1.Id, 2);
        placeholders[0].ResolvedTeamId = "real-a";
        placeholders[1].ResolvedTeamId = "real-b";

        // Phase 2 has resolved games with real team IDs
        phase2.Groups[0].AddTeam("real-a");
        phase2.Groups[0].AddTeam("real-b");

        var resolvedGames = new List<Game>
        {
            Game.Create(tournament.Id, phase2.Id, phase2.Groups[0].Id, 1,
                homeTeamId: "real-a", awayTeamId: "real-b")
        };

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(
            tournament.Id, phase2.Id)).Returns(resolvedGames);
        A.CallTo(() => _factory.FakeTeamRepository.GetBySourcePhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(placeholders);

        var client = _factory.CreateAuthenticatedClient("alice");
        await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/reopen", null);

        // Games should now have placeholder team IDs back
        resolvedGames[0].HomeTeamId.Should().Be(placeholders[0].Id);
        resolvedGames[0].AwayTeamId.Should().Be(placeholders[1].Id);
        // Placeholder teams should have ResolvedTeamId cleared
        placeholders[0].ResolvedTeamId.Should().BeNull();
        placeholders[1].ResolvedTeamId.Should().BeNull();
        // Groups should have placeholder team IDs restored (not empty)
        phase2.Groups[0].TeamIds.Should().Contain(placeholders[0].Id);
        phase2.Groups[0].TeamIds.Should().Contain(placeholders[1].Id);
    }

    [Fact]
    public async Task ReopenPhase_BlockedByCompletedNextPhaseGames_Returns409()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        var phase2 = structure.Phases[1];
        phase1.Complete();

        // Phase 2 has a completed game
        phase2.Groups[0].AddTeam("team1");
        phase2.Groups[0].AddTeam("team2");
        phase2.Activate();
        var completedGame = Game.Create(tournament.Id, phase2.Id, phase2.Groups[0].Id, 1,
            homeTeamId: "team1", awayTeamId: "team2");
        completedGame.RecordSimpleResult(2, 1);

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(
            tournament.Id, phase2.Id)).Returns(new List<Game> { completedGame });

        var client = _factory.CreateAuthenticatedClient("alice");
        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/reopen", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
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

    // --- Game Generation with Placeholder Teams ---

    [Fact]
    public async Task GenerateGames_Phase2WithPlaceholderTeams_ReturnsCreated()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        var phase2 = structure.Phases[1];
        var courts = new List<Court> { Court.Create("Court 1", tournament.Id) };

        // Create and assign placeholder teams to phase 2
        var placeholders = CreatePlaceholderTeams(phase1.Id, 4);
        foreach (var p in placeholders)
            phase2.Groups[0].AddTeam(p.Id);

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(courts);
        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(
            tournament.Id, phase2.Id)).Returns(false);
        A.CallTo(() => _factory.FakeGameRepository.CreateBatchAsync(A<IEnumerable<Game>>.Ignored))
            .ReturnsLazily((IEnumerable<Game> games) => Task.FromResult(games));
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(placeholders);

        var client = _factory.CreateAuthenticatedClient("alice");
        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase2.Id}/generate-schedule", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var games = await response.Content.ReadFromJsonAsync<List<GameDto>>();
        games.Should().NotBeNull();
        games!.Should().NotBeEmpty();

        // Games should have placeholder team names
        games.Should().OnlyContain(g =>
            g.HomeTeamName != null || g.HomeTeamPlaceholder != null);
    }

    // --- Placeholder Resolution on Late Phase Creation ---

    [Fact]
    public async Task AddPhase_WhenPreviousPhaseCompleted_ShouldResolvePlaceholdersImmediately()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase1 = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 2,
            groupWinners: 2, totalTeamsProceeding: 4);

        // Assign teams and complete phase 1
        for (var i = 0; i < 4; i++)
            phase1.Groups[0].AddTeam($"team{i + 1}");
        for (var i = 4; i < 8; i++)
            phase1.Groups[1].AddTeam($"team{i + 1}");
        phase1.Activate();
        phase1.Complete();

        var phase1Games = CreateCompletedGamesForPhase(tournament.Id, phase1);

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(phase1Games);

        var createdPlaceholders = new List<Team>();
        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(A<IEnumerable<Team>>.Ignored))
            .ReturnsLazily((IEnumerable<Team> teams) =>
            {
                var list = teams.ToList();
                createdPlaceholders.AddRange(list);
                return Task.FromResult<IEnumerable<Team>>(list);
            });

        var client = _factory.CreateAuthenticatedClient("alice");
        var dto = new PhaseDto(null, "Playoffs", "PlayoffElimination");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        createdPlaceholders.Should().HaveCount(4);
        createdPlaceholders.Should().OnlyContain(p => p.ResolvedTeamId != null);

        A.CallTo(() => _factory.FakeTeamRepository.UpdateAsync(A<Team>.Ignored))
            .MustHaveHappened(4, Times.Exactly);
    }

    [Fact]
    public async Task AddPhase_WhenPreviousPhaseInProgress_ShouldNotResolvePlaceholders()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase1 = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 2,
            groupWinners: 2);
        phase1.Activate();

        SetupFakes(tournament, structure);

        var createdPlaceholders = new List<Team>();
        A.CallTo(() => _factory.FakeTeamRepository.CreateBatchAsync(A<IEnumerable<Team>>.Ignored))
            .ReturnsLazily((IEnumerable<Team> teams) =>
            {
                var list = teams.ToList();
                createdPlaceholders.AddRange(list);
                return Task.FromResult<IEnumerable<Team>>(list);
            });

        var client = _factory.CreateAuthenticatedClient("alice");
        var dto = new PhaseDto(null, "Playoffs", "PlayoffElimination");
        var response = await client.PostAsJsonAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        createdPlaceholders.Should().HaveCount(4);
        createdPlaceholders.Should().OnlyContain(p => p.ResolvedTeamId == null);

        A.CallTo(() => _factory.FakeTeamRepository.UpdateAsync(A<Team>.Ignored))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task GenerateGames_ActivatesPhase()
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

    [Fact]
    public async Task CompletePhase_WithLevels_ShouldSeedLevel1AboveLevel2()
    {
        var tournament = CreateTestTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        var phase1 = structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 1,
            groupWinners: 1, startTime: new TimeOnly(9, 0), numberOfLevels: 2);
        // Phase 1: 2 levels × 1 group = 2 groups total
        var phase2 = structure.AddPhase("Playoffs", PhaseFormat.PlayoffElimination, 1,
            startTime: new TimeOnly(14, 0));

        var level1Group = phase1.Groups.First(g => g.LevelId == phase1.Levels[0].Id);
        var level2Group = phase1.Groups.First(g => g.LevelId == phase1.Levels[1].Id);

        // Level 1: L1-t1 and L1-t2, Level 2: L2-t1 (worse stats) and L2-t2
        level1Group.AddTeam("L1-t1");
        level1Group.AddTeam("L1-t2");
        level2Group.AddTeam("L2-t1");
        level2Group.AddTeam("L2-t2");

        phase1.Activate();

        // Create games: in each group, first team wins both
        var games = new List<Game>();
        // Level 1 group game: L1-t1 beats L1-t2
        var g1 = Game.Create(tournament.Id, phase1.Id, level1Group.Id, 1,
            homeTeamId: "L1-t1", awayTeamId: "L1-t2");
        g1.RecordSimpleResult(2, 0);
        games.Add(g1);
        // Level 2 group game: L2-t1 beats L2-t2
        var g2 = Game.Create(tournament.Id, phase1.Id, level2Group.Id, 1,
            homeTeamId: "L2-t1", awayTeamId: "L2-t2");
        g2.RecordSimpleResult(2, 0);
        games.Add(g2);

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase1.Id))
            .Returns(games);
        A.CallTo(() => _factory.FakeGameRepository.GetByPhaseIdAsync(tournament.Id, phase2.Id))
            .Returns(new List<Game>());

        var client = _factory.CreateAuthenticatedClient("alice");
        await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase1.Id}/complete", null);

        // Phase 2 should have 2 teams assigned (1 from each level)
        var phase2TeamIds = phase2.Groups.SelectMany(g => g.TeamIds).ToList();
        phase2TeamIds.Should().HaveCount(2);
        // Level 1 winner (L1-t1) should come first (seed 1), Level 2 winner (L2-t1) second
        phase2TeamIds[0].Should().Be("L1-t1");
        phase2TeamIds[1].Should().Be("L2-t1");
    }

    [Fact]
    public async Task GenerateGames_Phase2_ActivatesPhase()
    {
        var tournament = CreateTestTournament();
        var structure = CreateTwoPhaseStructure(tournament.Id);
        var phase1 = structure.Phases[0];
        var phase2 = structure.Phases[1];

        // Complete phase 1 so phase 2 is ready but stays New
        phase1.Complete();

        // Assign teams to phase 2
        phase2.Groups[0].AddTeam("team1");
        phase2.Groups[0].AddTeam("team2");
        phase2.Groups[0].AddTeam("team3");
        phase2.Groups[0].AddTeam("team4");

        var courts = new List<Court> { Court.Create("Court 1", tournament.Id) };
        var teams = new List<Team>
        {
            Team.Create("Team 1", 50, tournament.Id),
            Team.Create("Team 2", 40, tournament.Id),
            Team.Create("Team 3", 30, tournament.Id),
            Team.Create("Team 4", 20, tournament.Id)
        };

        SetupFakes(tournament, structure);
        A.CallTo(() => _factory.FakeCourtRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(courts);
        A.CallTo(() => _factory.FakeTeamRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(teams);
        A.CallTo(() => _factory.FakeGameRepository.GamesExistForPhaseAsync(
            tournament.Id, phase2.Id)).Returns(false);
        A.CallTo(() => _factory.FakeGameRepository.CreateBatchAsync(A<IEnumerable<Game>>.Ignored))
            .ReturnsLazily((IEnumerable<Game> games) => Task.FromResult(games));

        // Phase 2 should be New before game generation
        phase2.Status.Should().Be(PhaseStatus.New);

        var client = _factory.CreateAuthenticatedClient("alice");
        var response = await client.PostAsync(
            $"/api/tournaments/{tournament.Id}/structure/phases/{phase2.Id}/generate-schedule", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        phase2.Status.Should().Be(PhaseStatus.InProgress);
    }
}
