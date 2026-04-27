using FakeItEasy;
using FluentAssertions;
using FluentValidation;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Application.UnitTest.Services;

public class ScheduleGenerationServiceTests
{
    private const string TournamentId = "tournament-1";
    private const string PhaseId = "phase-1";

    private readonly IGameRepository _gameRepository = A.Fake<IGameRepository>();
    private readonly ICourtRepository _courtRepository = A.Fake<ICourtRepository>();
    private readonly ITournamentStructureRepository _structureRepository = A.Fake<ITournamentStructureRepository>();
    private readonly ScheduleGenerationService _sut;

    public ScheduleGenerationServiceTests()
    {
        _sut = new ScheduleGenerationService(_gameRepository, _courtRepository, _structureRepository);
    }

    private static Tournament CreateTournament(int? gameLength = 30)
    {
        var tournament = Tournament.Create("Cup", Discipline.Volleyball, "alice");
        tournament.Update("Cup", Discipline.Volleyball, DateTime.Parse("2026-06-01"),
            gameLength, null);
        return tournament;
    }

    private static TournamentStructure CreateStructure(
        int? groupCount = 1,
        TimeOnly? startTime = null,
        int teamCount = 3)
    {
        var structure = TournamentStructure.Create(TournamentId);
        var phase = structure.AddPhase("Pool", PhaseFormat.RoundRobin,
            numberOfGroups: groupCount ?? 1,
            startTime: startTime);
        phase.Id = PhaseId;
        phase.Groups[0].Id = "group-1";

        for (var i = 1; i <= teamCount; i++)
            phase.Groups[0].AddTeam($"team-{i}");

        return structure;
    }

    [Fact]
    public async Task Throws_when_game_length_is_missing()
    {
        var tournament = CreateTournament(gameLength: null);
        var structure = CreateStructure(startTime: new TimeOnly(9, 0));

        var act = async () => await _sut.GenerateAndScheduleAsync(
            tournament, TournamentId, PhaseId, structure);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "GameLength"));
    }

    [Fact]
    public async Task Throws_when_phase_start_time_is_missing()
    {
        var tournament = CreateTournament();
        var structure = CreateStructure(startTime: null);

        var act = async () => await _sut.GenerateAndScheduleAsync(
            tournament, TournamentId, PhaseId, structure);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "StartTime"));
    }

    [Fact]
    public async Task Throws_when_no_courts_exist()
    {
        var tournament = CreateTournament();
        var structure = CreateStructure(startTime: new TimeOnly(9, 0));
        A.CallTo(() => _courtRepository.GetByTournamentIdAsync(TournamentId))
            .Returns(new List<Court>());

        var act = async () => await _sut.GenerateAndScheduleAsync(
            tournament, TournamentId, PhaseId, structure);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Courts"));
    }

    [Fact]
    public async Task Throws_when_phase_has_no_teams()
    {
        var tournament = CreateTournament();
        var structure = CreateStructure(startTime: new TimeOnly(9, 0), teamCount: 0);
        A.CallTo(() => _courtRepository.GetByTournamentIdAsync(TournamentId))
            .Returns(new List<Court> { Court.Create("C1", TournamentId) });

        var act = async () => await _sut.GenerateAndScheduleAsync(
            tournament, TournamentId, PhaseId, structure);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Teams"));
    }

    [Fact]
    public async Task Throws_when_games_already_exist()
    {
        var tournament = CreateTournament();
        var structure = CreateStructure(startTime: new TimeOnly(9, 0));
        A.CallTo(() => _courtRepository.GetByTournamentIdAsync(TournamentId))
            .Returns(new List<Court> { Court.Create("C1", TournamentId) });
        A.CallTo(() => _gameRepository.GamesExistForPhaseAsync(TournamentId, PhaseId))
            .Returns(true);

        var act = async () => await _sut.GenerateAndScheduleAsync(
            tournament, TournamentId, PhaseId, structure);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Games"));
    }

    [Fact]
    public async Task Generates_schedules_and_activates_phase_when_prerequisites_met()
    {
        var tournament = CreateTournament();
        var structure = CreateStructure(startTime: new TimeOnly(9, 0));
        var courts = new List<Court> { Court.Create("C1", TournamentId) };
        A.CallTo(() => _courtRepository.GetByTournamentIdAsync(TournamentId)).Returns(courts);
        A.CallTo(() => _gameRepository.GamesExistForPhaseAsync(TournamentId, PhaseId)).Returns(false);
        A.CallTo(() => _gameRepository.CreateBatchAsync(A<IEnumerable<Game>>._))
            .ReturnsLazily((IEnumerable<Game> games) => games);

        var saved = await _sut.GenerateAndScheduleAsync(tournament, TournamentId, PhaseId, structure);

        saved.Should().NotBeEmpty();
        structure.GetPhase(PhaseId).Status.Should().Be(PhaseStatus.InProgress);
        A.CallTo(() => _structureRepository.UpdateAsync(structure)).MustHaveHappened();
    }

    [Fact]
    public async Task Phase_becomes_Scheduled_when_previous_phase_progression_is_unresolved()
    {
        var tournament = CreateTournament();

        var structure = TournamentStructure.Create(TournamentId);
        var phase1 = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1,
            groupWinners: 1, startTime: new TimeOnly(9, 0));
        var phase2 = structure.AddPhase("Finals", PhaseFormat.RoundRobin, 1,
            startTime: new TimeOnly(12, 0));
        phase2.Id = PhaseId;
        phase2.Groups[0].Id = "g-2";
        phase2.Groups[0].AddTeam("t-1");
        phase2.Groups[0].AddTeam("t-2");

        A.CallTo(() => _courtRepository.GetByTournamentIdAsync(TournamentId))
            .Returns(new List<Court> { Court.Create("C1", TournamentId) });
        A.CallTo(() => _gameRepository.CreateBatchAsync(A<IEnumerable<Game>>._))
            .ReturnsLazily((IEnumerable<Game> games) => games);

        await _sut.GenerateAndScheduleAsync(tournament, TournamentId, PhaseId, structure);

        structure.GetPhase(PhaseId).Status.Should().Be(PhaseStatus.Scheduled);
    }

    // --- Custom phase activation ---

    private static TournamentStructure CreateCustomStructure(int teamCount = 2)
    {
        var structure = TournamentStructure.Create(TournamentId);
        var phase = structure.AddPhase("Custom", PhaseFormat.Custom, numberOfGroups: 1);
        phase.Id = PhaseId;
        phase.Groups[0].Id = "group-1";
        for (var i = 1; i <= teamCount; i++)
            phase.Groups[0].AddTeam($"team-{i}");
        return structure;
    }

    [Fact]
    public async Task Custom_Activates_phase_without_games_courts_or_start_time()
    {
        var tournament = CreateTournament(gameLength: null);
        var structure = CreateCustomStructure();

        var saved = await _sut.GenerateAndScheduleAsync(
            tournament, TournamentId, PhaseId, structure);

        saved.Should().BeEmpty();
        structure.GetPhase(PhaseId).Status.Should().Be(PhaseStatus.InProgress);
        A.CallTo(() => _gameRepository.CreateBatchAsync(A<IEnumerable<Game>>._))
            .MustNotHaveHappened();
        A.CallTo(() => _courtRepository.GetByTournamentIdAsync(TournamentId))
            .MustNotHaveHappened();
        A.CallTo(() => _structureRepository.UpdateAsync(structure)).MustHaveHappened();
    }

    [Fact]
    public async Task Custom_Throws_when_any_group_has_no_teams()
    {
        var tournament = CreateTournament();
        var structure = CreateCustomStructure(teamCount: 0);

        var act = async () => await _sut.GenerateAndScheduleAsync(
            tournament, TournamentId, PhaseId, structure);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Teams"));
    }

    [Fact]
    public async Task Custom_Phase_becomes_Scheduled_when_previous_phase_progression_is_unresolved()
    {
        var tournament = CreateTournament();

        var structure = TournamentStructure.Create(TournamentId);
        structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1,
            groupWinners: 1, startTime: new TimeOnly(9, 0));
        var phase2 = structure.AddPhase("Custom Finals", PhaseFormat.Custom, 1);
        phase2.Id = PhaseId;
        phase2.Groups[0].Id = "g-2";
        phase2.Groups[0].AddTeam("t-1");

        await _sut.GenerateAndScheduleAsync(tournament, TournamentId, PhaseId, structure);

        structure.GetPhase(PhaseId).Status.Should().Be(PhaseStatus.Scheduled);
    }
}
