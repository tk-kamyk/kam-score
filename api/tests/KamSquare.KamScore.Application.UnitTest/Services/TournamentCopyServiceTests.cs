using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Application.UnitTest.Services;

public class TournamentCopyServiceTests
{
    private readonly ITournamentRepository _tournamentRepository = A.Fake<ITournamentRepository>();
    private readonly ITournamentStructureRepository _structureRepository = A.Fake<ITournamentStructureRepository>();
    private readonly ITeamRepository _teamRepository = A.Fake<ITeamRepository>();
    private readonly ICourtRepository _courtRepository = A.Fake<ICourtRepository>();
    private readonly IGameRepository _gameRepository = A.Fake<IGameRepository>();
    private readonly TournamentCopyService _sut;

    public TournamentCopyServiceTests()
    {
        _sut = new TournamentCopyService(
            _tournamentRepository,
            _structureRepository,
            _teamRepository,
            _courtRepository,
            _gameRepository);
    }

    private Tournament CreateSourceTournament()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "alice");
        tournament.Update("Summer Cup", Discipline.Volleyball, DateTime.Parse("2026-06-01"), 60,
            new GameConditions { BestOfSets = 3, PointsPerSet = [25, 25, 15] });
        return tournament;
    }

    private TournamentStructure CreateSourceStructure(string tournamentId)
    {
        var structure = TournamentStructure.Create(tournamentId);
        structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 2,
            groupWinners: 2, startTime: new TimeOnly(9, 0));
        structure.AddPhase("Playoffs", PhaseFormat.PlayoffElimination, 1,
            startTime: new TimeOnly(14, 0));
        return structure;
    }

    private List<Team> CreateSourceTeams(string tournamentId, int count = 8)
    {
        return Team.GenerateSeedTeams(count, 1, tournamentId)
            .Select(t => { t.IsPlaceholder = false; return t; })
            .ToList();
    }

    private List<Court> CreateSourceCourts(string tournamentId)
    {
        return
        [
            Court.Create("Main", tournamentId),
            Court.Create("Side A", tournamentId),
            Court.Create("Side B", tournamentId)
        ];
    }

    private void SetupFakesForCopy(Tournament source, TournamentStructure structure,
        List<Team> teams, List<Court> courts)
    {
        A.CallTo(() => _tournamentRepository.GetByIdAsync(source.Id))
            .Returns(source);
        A.CallTo(() => _structureRepository.GetByTournamentIdAsync(source.Id))
            .Returns(structure);
        A.CallTo(() => _teamRepository.GetByTournamentIdAsync(source.Id))
            .Returns(teams);
        A.CallTo(() => _courtRepository.GetByTournamentIdAsync(source.Id))
            .Returns(courts);
        A.CallTo(() => _tournamentRepository.CreateAsync(A<Tournament>.Ignored))
            .ReturnsLazily((Tournament t) => Task.FromResult(t));
        A.CallTo(() => _structureRepository.CreateAsync(A<TournamentStructure>.Ignored))
            .ReturnsLazily((TournamentStructure s) => Task.FromResult(s));
        A.CallTo(() => _teamRepository.CreateBatchAsync(A<IEnumerable<Team>>.Ignored))
            .ReturnsLazily((IEnumerable<Team> t) => Task.FromResult(t));
        A.CallTo(() => _courtRepository.CreateBatchAsync(A<IEnumerable<Court>>.Ignored))
            .ReturnsLazily((IEnumerable<Court> c) => Task.FromResult(c));
        A.CallTo(() => _gameRepository.CreateBatchAsync(A<IEnumerable<Game>>.Ignored))
            .ReturnsLazily((IEnumerable<Game> g) => Task.FromResult(g));
    }

    [Fact]
    public async Task CopyAsync_ShouldCopyTournamentSettings()
    {
        var source = CreateSourceTournament();
        var structure = CreateSourceStructure(source.Id);
        var teams = CreateSourceTeams(source.Id);
        var courts = CreateSourceCourts(source.Id);
        SetupFakesForCopy(source, structure, teams, courts);

        var result = await _sut.CopyAsync(source.Id, "Winter Cup", "bob");

        result.Name.Should().Be("Winter Cup");
        result.Discipline.Should().Be(Discipline.Volleyball);
        result.GameLength.Should().Be(60);
        result.GameConditions.Should().NotBeNull();
        result.GameConditions!.BestOfSets.Should().Be(3);
        result.OwnerId.Should().Be("bob");
        result.TournamentCode.Should().NotBe(source.TournamentCode);
    }

    [Fact]
    public async Task CopyAsync_ShouldCreateCourtsWithSameNames()
    {
        var source = CreateSourceTournament();
        var structure = CreateSourceStructure(source.Id);
        var teams = CreateSourceTeams(source.Id);
        var courts = CreateSourceCourts(source.Id);
        SetupFakesForCopy(source, structure, teams, courts);

        await _sut.CopyAsync(source.Id, "Winter Cup", "bob");

        A.CallTo(() => _courtRepository.CreateBatchAsync(
                A<IEnumerable<Court>>.That.Matches(c =>
                    c.Count() == 3 &&
                    c.Any(ct => ct.Name == "Main") &&
                    c.Any(ct => ct.Name == "Side A") &&
                    c.Any(ct => ct.Name == "Side B"))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CopyAsync_ShouldCreateSeedTeamsMatchingRealTeamCount()
    {
        var source = CreateSourceTournament();
        var structure = CreateSourceStructure(source.Id);
        var teams = CreateSourceTeams(source.Id, 8);
        // Add 4 placeholder teams that should NOT be counted
        var placeholders = Enumerable.Range(1, 4).Select(i =>
            Team.CreatePlaceholder($"Placeholder {i}", source.Id, "phase-1", i)).ToList();
        teams.AddRange(placeholders);
        var courts = CreateSourceCourts(source.Id);
        SetupFakesForCopy(source, structure, teams, courts);

        await _sut.CopyAsync(source.Id, "Winter Cup", "bob");

        A.CallTo(() => _teamRepository.CreateBatchAsync(
                A<IEnumerable<Team>>.That.Matches(t =>
                    t.Count() == 8 &&
                    t.All(team => team.Name.StartsWith("Seed")))))
            .MustHaveHappened();
    }

    [Fact]
    public async Task CopyAsync_ShouldCopyStructurePhases()
    {
        var source = CreateSourceTournament();
        var structure = CreateSourceStructure(source.Id);
        var teams = CreateSourceTeams(source.Id);
        var courts = CreateSourceCourts(source.Id);
        SetupFakesForCopy(source, structure, teams, courts);

        await _sut.CopyAsync(source.Id, "Winter Cup", "bob");

        A.CallTo(() => _structureRepository.CreateAsync(
                A<TournamentStructure>.That.Matches(s =>
                    s.Phases.Count == 2 &&
                    s.Phases[0].Name == "Group Stage" &&
                    s.Phases[0].Format == PhaseFormat.RoundRobin &&
                    s.Phases[0].Groups.Count == 2 &&
                    s.Phases[0].GroupWinners == 2 &&
                    s.Phases[1].Name == "Playoffs" &&
                    s.Phases[1].Format == PhaseFormat.PlayoffElimination &&
                    s.Phases[1].Groups.Count == 1)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CopyAsync_ShouldGenerateGamesForAllPhases()
    {
        var source = CreateSourceTournament();
        var structure = CreateSourceStructure(source.Id);
        var teams = CreateSourceTeams(source.Id);
        var courts = CreateSourceCourts(source.Id);
        SetupFakesForCopy(source, structure, teams, courts);

        await _sut.CopyAsync(source.Id, "Winter Cup", "bob");

        // Games should be created (at least once for the phases that have prerequisites)
        A.CallTo(() => _gameRepository.CreateBatchAsync(A<IEnumerable<Game>>.Ignored))
            .MustHaveHappened();
    }

    [Fact]
    public async Task CopyAsync_ShouldSetPhaseStatuses()
    {
        var source = CreateSourceTournament();
        var structure = CreateSourceStructure(source.Id);
        var teams = CreateSourceTeams(source.Id);
        var courts = CreateSourceCourts(source.Id);
        SetupFakesForCopy(source, structure, teams, courts);

        await _sut.CopyAsync(source.Id, "Winter Cup", "bob");

        A.CallTo(() => _structureRepository.CreateAsync(
                A<TournamentStructure>.That.Matches(s =>
                    s.Phases[0].Status == PhaseStatus.InProgress &&
                    s.Phases[1].Status == PhaseStatus.Scheduled)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CopyAsync_ShouldGeneratePlaceholderTeamsForPhase2()
    {
        var source = CreateSourceTournament();
        var structure = CreateSourceStructure(source.Id);
        var teams = CreateSourceTeams(source.Id);
        var courts = CreateSourceCourts(source.Id);
        SetupFakesForCopy(source, structure, teams, courts);

        await _sut.CopyAsync(source.Id, "Winter Cup", "bob");

        // Placeholder teams for phase 2 (4 teams = groupWinners 2 * 2 groups)
        A.CallTo(() => _teamRepository.CreateBatchAsync(
                A<IEnumerable<Team>>.That.Matches(t =>
                    t.Any(team => team.IsPlaceholder))))
            .MustHaveHappened();
    }

    [Fact]
    public async Task CopyAsync_SourceNotFound_ShouldThrowNotFoundException()
    {
        A.CallTo(() => _tournamentRepository.GetByIdAsync("nonexistent"))
            .Returns((Tournament?)null);

        var act = () => _sut.CopyAsync("nonexistent", "Copy", "bob");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CopyAsync_SourceHasNoStructure_ShouldThrowNotFoundException()
    {
        var source = CreateSourceTournament();
        A.CallTo(() => _tournamentRepository.GetByIdAsync(source.Id))
            .Returns(source);
        A.CallTo(() => _structureRepository.GetByTournamentIdAsync(source.Id))
            .Returns((TournamentStructure?)null);

        var act = () => _sut.CopyAsync(source.Id, "Copy", "bob");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CopyAsync_PhaseWithoutStartTime_ShouldSkipGameGeneration()
    {
        var source = CreateSourceTournament();
        var structure = TournamentStructure.Create(source.Id);
        structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 2); // no startTime
        var teams = CreateSourceTeams(source.Id);
        var courts = CreateSourceCourts(source.Id);
        SetupFakesForCopy(source, structure, teams, courts);

        await _sut.CopyAsync(source.Id, "Winter Cup", "bob");

        A.CallTo(() => _structureRepository.CreateAsync(
                A<TournamentStructure>.That.Matches(s =>
                    s.Phases[0].Status == PhaseStatus.New)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CopyAsync_WhenCourtCreationFails_ShouldCleanupTournament()
    {
        var source = CreateSourceTournament();
        var structure = CreateSourceStructure(source.Id);
        var teams = CreateSourceTeams(source.Id);
        var courts = CreateSourceCourts(source.Id);
        SetupFakesForCopy(source, structure, teams, courts);
        A.CallTo(() => _courtRepository.CreateBatchAsync(A<IEnumerable<Court>>.Ignored))
            .Throws(new InvalidOperationException("Cosmos DB error"));

        var act = () => _sut.CopyAsync(source.Id, "Winter Cup", "bob");

        await act.Should().ThrowAsync<InvalidOperationException>();
        A.CallTo(() => _tournamentRepository.DeleteAsync(A<string>.Ignored, "bob"))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _gameRepository.DeleteByTournamentIdAsync(A<string>.Ignored))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _teamRepository.DeleteByTournamentIdAsync(A<string>.Ignored))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _courtRepository.DeleteByTournamentIdAsync(A<string>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CopyAsync_SourceWithNoTeamsAndNoCourts_ShouldSucceed()
    {
        var source = CreateSourceTournament();
        var structure = TournamentStructure.Create(source.Id);
        structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 1,
            startTime: new TimeOnly(9, 0));
        SetupFakesForCopy(source, structure, [], []);

        var result = await _sut.CopyAsync(source.Id, "Empty Cup", "bob");

        result.Name.Should().Be("Empty Cup");
        A.CallTo(() => _courtRepository.CreateBatchAsync(A<IEnumerable<Court>>.Ignored))
            .MustNotHaveHappened();
        A.CallTo(() => _teamRepository.CreateBatchAsync(
                A<IEnumerable<Team>>.That.Matches(t => !t.Any())))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task CopyAsync_WithLevels_ShouldCopyLevelConfiguration()
    {
        var source = CreateSourceTournament();
        var structure = TournamentStructure.Create(source.Id);
        structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 2,
            groupWinners: 2, startTime: new TimeOnly(9, 0), numberOfLevels: 2);
        var teams = CreateSourceTeams(source.Id, 16);
        var courts = CreateSourceCourts(source.Id);
        SetupFakesForCopy(source, structure, teams, courts);

        await _sut.CopyAsync(source.Id, "Winter Cup", "bob");

        A.CallTo(() => _structureRepository.CreateAsync(
                A<TournamentStructure>.That.Matches(s =>
                    s.Phases[0].Levels.Count == 2 &&
                    s.Phases[0].Groups.Count == 4))) // 2 groups per level * 2 levels
            .MustHaveHappenedOnceExactly();
    }
}
