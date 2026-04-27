using FakeItEasy;
using FluentAssertions;
using FluentValidation;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Application.UnitTest.Services;

public class PhaseCompletionServiceTests
{
    private const string TournamentId = "t-1";

    private readonly IGameRepository _gameRepository = A.Fake<IGameRepository>();
    private readonly ITeamRepository _teamRepository = A.Fake<ITeamRepository>();
    private readonly ITournamentStructureRepository _structureRepository = A.Fake<ITournamentStructureRepository>();
    private readonly PhaseCompletionService _sut;

    public PhaseCompletionServiceTests()
    {
        _sut = new PhaseCompletionService(_gameRepository, _teamRepository, _structureRepository);
    }

    private static TournamentStructure CreateStructureWithPhase(PhaseStatus status = PhaseStatus.InProgress)
    {
        var structure = TournamentStructure.Create(TournamentId);
        var phase = structure.AddPhase("Groups", PhaseFormat.RoundRobin, 1,
            startTime: new TimeOnly(9, 0));
        phase.Id = "phase-1";
        phase.Groups[0].Id = "group-1";
        phase.Groups[0].AddTeam("team-1");
        phase.Groups[0].AddTeam("team-2");

        switch (status)
        {
            case PhaseStatus.InProgress:
                phase.Activate();
                break;
            case PhaseStatus.Completed:
                phase.Activate();
                phase.Complete();
                break;
        }

        return structure;
    }

    // --- CompletePhaseAsync ---

    [Fact]
    public async Task CompletePhase_throws_when_phase_is_not_InProgress()
    {
        var structure = CreateStructureWithPhase(PhaseStatus.New);

        var act = async () => await _sut.CompletePhaseAsync(TournamentId, "phase-1", structure);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Status"));
    }

    [Fact]
    public async Task CompletePhase_throws_when_games_are_not_all_completed()
    {
        var structure = CreateStructureWithPhase();
        var game = Game.Create(TournamentId, "phase-1", "group-1", round: 1,
            homeTeamId: "team-1", awayTeamId: "team-2");
        A.CallTo(() => _gameRepository.GetByPhaseIdAsync(TournamentId, "phase-1"))
            .Returns(new List<Game> { game });

        var act = async () => await _sut.CompletePhaseAsync(TournamentId, "phase-1", structure);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Games"));
    }

    [Fact]
    public async Task CompletePhase_marks_phase_completed_when_all_games_done()
    {
        var structure = CreateStructureWithPhase();
        var game = Game.Create(TournamentId, "phase-1", "group-1", round: 1,
            homeTeamId: "team-1", awayTeamId: "team-2");
        game.RecordSimpleResult(2, 0);
        A.CallTo(() => _gameRepository.GetByPhaseIdAsync(TournamentId, "phase-1"))
            .Returns(new List<Game> { game });

        var result = await _sut.CompletePhaseAsync(TournamentId, "phase-1", structure);

        result.Status.Should().Be(PhaseStatus.Completed);
        A.CallTo(() => _structureRepository.UpdateAsync(structure)).MustHaveHappened();
    }

    // --- DeletePhaseGamesAsync ---

    [Fact]
    public async Task DeletePhaseGames_resets_InProgress_phase_to_New()
    {
        var structure = CreateStructureWithPhase();

        await _sut.DeletePhaseGamesAsync(structure, "phase-1", TournamentId);

        structure.GetPhase("phase-1").Status.Should().Be(PhaseStatus.New);
        A.CallTo(() => _gameRepository.DeleteByPhaseIdAsync(TournamentId, "phase-1"))
            .MustHaveHappened();
        A.CallTo(() => _structureRepository.UpdateAsync(structure)).MustHaveHappened();
    }

    [Fact]
    public async Task DeletePhaseGames_keeps_New_phase_in_New_and_does_not_persist_structure()
    {
        var structure = CreateStructureWithPhase(PhaseStatus.New);

        await _sut.DeletePhaseGamesAsync(structure, "phase-1", TournamentId);

        structure.GetPhase("phase-1").Status.Should().Be(PhaseStatus.New);
        A.CallTo(() => _structureRepository.UpdateAsync(structure)).MustNotHaveHappened();
    }

    // --- ReopenPhaseAsync ---

    [Fact]
    public async Task ReopenPhase_throws_when_phase_is_not_Completed()
    {
        var structure = CreateStructureWithPhase(PhaseStatus.InProgress);

        var act = async () => await _sut.ReopenPhaseAsync(TournamentId, "phase-1", structure);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Status"));
    }

    [Fact]
    public async Task ReopenPhase_blocks_when_next_phase_has_completed_games()
    {
        var structure = CreateStructureWithPhase(PhaseStatus.Completed);
        var nextPhase = structure.AddPhase("Finals", PhaseFormat.RoundRobin, 1,
            startTime: new TimeOnly(12, 0));
        nextPhase.Id = "phase-2";

        var completedGame = Game.Create(TournamentId, "phase-2", "g", 1,
            homeTeamId: "a", awayTeamId: "b");
        completedGame.RecordSimpleResult(2, 0);
        A.CallTo(() => _gameRepository.GetByPhaseIdAsync(TournamentId, "phase-2"))
            .Returns(new List<Game> { completedGame });

        var act = async () => await _sut.ReopenPhaseAsync(TournamentId, "phase-1", structure);

        await act.Should().ThrowAsync<PhaseStateException>();
    }

    [Fact]
    public async Task ReopenPhase_returns_phase_to_InProgress_when_no_blocker()
    {
        var structure = CreateStructureWithPhase(PhaseStatus.Completed);

        var result = await _sut.ReopenPhaseAsync(TournamentId, "phase-1", structure);

        result.Status.Should().Be(PhaseStatus.InProgress);
        A.CallTo(() => _structureRepository.UpdateAsync(structure)).MustHaveHappened();
    }

    // --- Custom phase completion ---

    private static TournamentStructure CreateCustomPhaseStructure(
        bool withCompleteOrderings,
        PhaseStatus status = PhaseStatus.InProgress)
    {
        var structure = TournamentStructure.Create(TournamentId);
        var phase = structure.AddPhase("Custom", PhaseFormat.Custom, 1);
        phase.Id = "phase-1";
        phase.Groups[0].Id = "group-1";
        phase.Groups[0].AddTeam("team-1");
        phase.Groups[0].AddTeam("team-2");

        if (withCompleteOrderings)
            phase.Groups[0].SetManualStandingOrder(["team-2", "team-1"]);

        switch (status)
        {
            case PhaseStatus.InProgress:
                phase.Activate();
                break;
            case PhaseStatus.Completed:
                phase.Activate();
                phase.Complete();
                break;
        }

        return structure;
    }

    [Fact]
    public async Task CompletePhase_Custom_throws_when_any_group_has_incomplete_order()
    {
        var structure = CreateCustomPhaseStructure(withCompleteOrderings: false);

        var act = async () => await _sut.CompletePhaseAsync(TournamentId, "phase-1", structure);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Standings"));
    }

    [Fact]
    public async Task CompletePhase_Custom_marks_completed_when_all_groups_have_order()
    {
        var structure = CreateCustomPhaseStructure(withCompleteOrderings: true);

        var result = await _sut.CompletePhaseAsync(TournamentId, "phase-1", structure);

        result.Status.Should().Be(PhaseStatus.Completed);
        A.CallTo(() => _structureRepository.UpdateAsync(structure)).MustHaveHappened();
    }

    [Fact]
    public async Task CompletePhase_Custom_doesNotQueryGamesRepository()
    {
        var structure = CreateCustomPhaseStructure(withCompleteOrderings: true);

        await _sut.CompletePhaseAsync(TournamentId, "phase-1", structure);

        A.CallTo(() => _gameRepository.GetByPhaseIdAsync(TournamentId, "phase-1"))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task CompletePhase_Custom_seedsGroupWinnersIntoTopLevelOfNextPhase()
    {
        var structure = TournamentStructure.Create(TournamentId);

        var sourcePhase = structure.AddPhase("Custom", PhaseFormat.Custom, numberOfGroups: 2,
            totalTeamsProceeding: 6);
        sourcePhase.Id = "phase-1";
        sourcePhase.Groups[0].Id = "group-a";
        sourcePhase.Groups[0].AddTeam("a1");
        sourcePhase.Groups[0].AddTeam("a2");
        sourcePhase.Groups[0].AddTeam("a3");
        sourcePhase.Groups[0].SetManualStandingOrder(["a1", "a2", "a3"]);
        sourcePhase.Groups[1].Id = "group-b";
        sourcePhase.Groups[1].AddTeam("b1");
        sourcePhase.Groups[1].AddTeam("b2");
        sourcePhase.Groups[1].AddTeam("b3");
        sourcePhase.Groups[1].SetManualStandingOrder(["b1", "b2", "b3"]);
        sourcePhase.Activate();

        var nextPhase = structure.AddPhase("Finals", PhaseFormat.RoundRobin, numberOfGroups: 1,
            numberOfLevels: 2);
        nextPhase.Id = "phase-2";

        await _sut.CompletePhaseAsync(TournamentId, "phase-1", structure);

        // Seeding cross-group ranks the 6 teams as [a1, b1, a2, b2, a3, b3]
        // (Custom format: position-major across groups, group-stable on ties).
        // DistributeAcrossLevels then chunks 3-per-level into 2 levels:
        //   Level 1 chunk = [a1, b1, a2]
        //   Level 2 chunk = [b2, a3, b3]
        // Each level has one group, so SnakeDraftIntoGroups stores the chunk
        // verbatim in that group.
        var orderedLevels = nextPhase.Levels.OrderBy(l => l.Order).ToList();
        var level1Group = nextPhase.Groups.Single(g => g.LevelId == orderedLevels[0].Id);
        var level2Group = nextPhase.Groups.Single(g => g.LevelId == orderedLevels[1].Id);

        level1Group.TeamIds.Should().HaveCount(3);
        level2Group.TeamIds.Should().HaveCount(3);
        level1Group.TeamIds.Should().Equal("a1", "b1", "a2");
        level2Group.TeamIds.Should().Equal("b2", "a3", "b3");
    }
}
