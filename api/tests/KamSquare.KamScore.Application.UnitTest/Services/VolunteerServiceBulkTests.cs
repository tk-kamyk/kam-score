using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Application.UnitTest.Services;

public class VolunteerServiceBulkTests
{
    private const string TournamentId = "t-1";

    private readonly IVolunteerRepository _volunteerRepository = A.Fake<IVolunteerRepository>();
    private readonly ITournamentStructureRepository _structureRepository = A.Fake<ITournamentStructureRepository>();
    private readonly IGameRepository _gameRepository = A.Fake<IGameRepository>();
    private readonly ITeamRepository _teamRepository = A.Fake<ITeamRepository>();
    private readonly VolunteerService _sut;

    public VolunteerServiceBulkTests()
    {
        _sut = new VolunteerService(_volunteerRepository, _structureRepository, _gameRepository, _teamRepository);
        // Default: repository returns whatever volunteer is passed in
        A.CallTo(() => _volunteerRepository.UpdateAsync(A<Volunteer>.Ignored))
            .ReturnsLazily((Volunteer v) => Task.FromResult(v));
    }

    private static Tournament CreateTournament(int? gameLength = 20)
    {
        var tournament = Tournament.Create("Cup", Discipline.Volleyball, "alice");
        tournament.Update("Cup", Discipline.Volleyball, DateTime.Parse("2026-06-01"), gameLength, null);
        return tournament;
    }

    private static TournamentStructure CreateStructureWithPhase(
        string tournamentId,
        string phaseName = "Pool",
        TimeOnly? startTime = null)
    {
        var structure = TournamentStructure.Create(tournamentId);
        structure.AddPhase(phaseName, PhaseFormat.RoundRobin, 1, startTime: startTime ?? new TimeOnly(9, 0));
        return structure;
    }

    private void SetupStructureAndGames(TournamentStructure structure, IEnumerable<Game>? games = null)
    {
        A.CallTo(() => _structureRepository.GetByTournamentIdAsync(structure.TournamentId))
            .Returns(structure);
        A.CallTo(() => _gameRepository.GetByTournamentIdAsync(structure.TournamentId))
            .Returns(games ?? Array.Empty<Game>());
    }

    // --- ClearShiftGroupAssignmentsAsync ---

    [Fact]
    public async Task ClearShiftGroup_RemovesOnlyMatchingAssignments()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);

        var volunteer = Volunteer.Create("Alice", tournament.Id);
        volunteer.AssignShift("Pool", new TimeOnly(9, 0));
        volunteer.AssignShift("Pool", new TimeOnly(9, 20));
        volunteer.AssignShift("Set-up", null);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { volunteer });

        await _sut.ClearShiftGroupAssignmentsAsync(tournament, "Pool");

        volunteer.Assignments.Should().ContainSingle()
            .Which.Should().Be(new ShiftAssignment("Set-up", null));
    }

    [Fact]
    public async Task ClearShiftGroup_PreservesAssignmentsForOtherVolunteers()
    {
        var tournament = CreateTournament();
        var structure = TournamentStructure.Create(tournament.Id);
        structure.AddPhase("Pool", PhaseFormat.RoundRobin, 1, startTime: new TimeOnly(9, 0));
        structure.AddPhase("Bracket", PhaseFormat.RoundRobin, 1, startTime: new TimeOnly(13, 0));
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        alice.AssignShift("Pool", new TimeOnly(9, 0));
        var bob = Volunteer.Create("Bob", tournament.Id);
        bob.AssignShift("Bracket", new TimeOnly(13, 0));
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { alice, bob });

        await _sut.ClearShiftGroupAssignmentsAsync(tournament, "Pool");

        alice.Assignments.Should().BeEmpty();
        bob.Assignments.Should().ContainSingle()
            .Which.ShiftGroup.Should().Be("Bracket");
    }

    [Fact]
    public async Task ClearShiftGroup_IsNoOp_WhenNoMatchingAssignments()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);

        var volunteer = Volunteer.Create("Alice", tournament.Id);
        volunteer.AssignShift("Set-up", null);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { volunteer });

        await _sut.ClearShiftGroupAssignmentsAsync(tournament, "Pool");

        volunteer.Assignments.Should().ContainSingle()
            .Which.Should().Be(new ShiftAssignment("Set-up", null));
        // No update call expected when nothing changed
        A.CallTo(() => _volunteerRepository.UpdateAsync(A<Volunteer>.Ignored))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ClearShiftGroup_Throws_WhenShiftGroupUnknown()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Volunteer>());

        var act = async () => await _sut.ClearShiftGroupAssignmentsAsync(tournament, "Unknown");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ClearShiftGroup_WorksForSpecialGroup()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);

        var volunteer = Volunteer.Create("Alice", tournament.Id);
        volunteer.AssignShift("Set-up", null);
        volunteer.AssignShift("Pool", new TimeOnly(9, 0));
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { volunteer });

        await _sut.ClearShiftGroupAssignmentsAsync(tournament, "Set-up");

        volunteer.Assignments.Should().ContainSingle()
            .Which.ShiftGroup.Should().Be("Pool");
    }

    // --- AutoAssignShiftGroupAsync ---

    [Fact]
    public async Task AutoAssign_FillsEachShiftToRequestedCount()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        // Make sure Pool yields exactly two shifts via a follow-up phase
        structure.AddPhase("Bracket", PhaseFormat.RoundRobin, 1, startTime: new TimeOnly(9, 40));
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        var bob = Volunteer.Create("Bob", tournament.Id);
        var carol = Volunteer.Create("Carol", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { alice, bob, carol });

        await _sut.AutoAssignShiftGroupAsync(tournament, "Pool", 2);

        var poolAssignments = new[] { alice, bob, carol }
            .SelectMany(v => v.Assignments.Where(a => a.ShiftGroup == "Pool"))
            .ToList();
        // Two shifts × 2 = 4 assignments total
        poolAssignments.Should().HaveCount(4);
        poolAssignments.GroupBy(a => a.ShiftTime).Should().AllSatisfy(g => g.Count().Should().Be(2));
    }

    [Fact]
    public async Task AutoAssign_TopsUp_WithoutDisplacingExistingAssignments()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        structure.AddPhase("Bracket", PhaseFormat.RoundRobin, 1, startTime: new TimeOnly(9, 40));
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        alice.AssignShift("Pool", new TimeOnly(9, 0));
        var bob = Volunteer.Create("Bob", tournament.Id);
        var carol = Volunteer.Create("Carol", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { alice, bob, carol });

        await _sut.AutoAssignShiftGroupAsync(tournament, "Pool", 2);

        // Alice's existing Pool 09:00 must remain
        alice.Assignments.Should().Contain(new ShiftAssignment("Pool", new TimeOnly(9, 0)));
        // Both 09:00 and 09:20 should have 2 assignees
        var nineHundred = new[] { alice, bob, carol }
            .Where(v => v.Assignments.Any(a => a.ShiftGroup == "Pool" && a.ShiftTime == new TimeOnly(9, 0)))
            .ToList();
        nineHundred.Should().HaveCount(2);
    }

    [Fact]
    public async Task AutoAssign_SkipsSlots_AlreadyAtOrAboveRequestedCount()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        structure.AddPhase("Bracket", PhaseFormat.RoundRobin, 1, startTime: new TimeOnly(9, 40));
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        alice.AssignShift("Pool", new TimeOnly(9, 0));
        var bob = Volunteer.Create("Bob", tournament.Id);
        bob.AssignShift("Pool", new TimeOnly(9, 0));
        var carol = Volunteer.Create("Carol", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { alice, bob, carol });

        await _sut.AutoAssignShiftGroupAsync(tournament, "Pool", 2);

        // Slot 09:00 already has 2 — should not get a third assignee
        var nineHundred = new[] { alice, bob, carol }
            .Count(v => v.Assignments.Any(a => a.ShiftGroup == "Pool" && a.ShiftTime == new TimeOnly(9, 0)));
        nineHundred.Should().Be(2);
        // Slot 09:20 should still get filled to 2
        var nineTwenty = new[] { alice, bob, carol }
            .Count(v => v.Assignments.Any(a => a.ShiftGroup == "Pool" && a.ShiftTime == new TimeOnly(9, 20)));
        nineTwenty.Should().Be(2);
    }

    [Fact]
    public async Task AutoAssign_ReRanksBetweenShifts_FavoringLowerShiftCount()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        structure.AddPhase("Bracket", PhaseFormat.RoundRobin, 1, startTime: new TimeOnly(9, 40));
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        var bob = Volunteer.Create("Bob", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { alice, bob });

        // N=1, 2 slots, 2 candidates — each should get exactly one slot if re-rank works
        await _sut.AutoAssignShiftGroupAsync(tournament, "Pool", 1);

        alice.Assignments.Where(a => a.ShiftGroup == "Pool").Should().ContainSingle();
        bob.Assignments.Where(a => a.ShiftGroup == "Pool").Should().ContainSingle();
        // They should be on different slots
        alice.Assignments.First(a => a.ShiftGroup == "Pool").ShiftTime
            .Should().NotBe(bob.Assignments.First(a => a.ShiftGroup == "Pool").ShiftTime);
    }

    [Fact]
    public async Task AutoAssign_PartialFill_WhenCandidatePoolSmallerThanRequested()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        structure.AddPhase("Bracket", PhaseFormat.RoundRobin, 1, startTime: new TimeOnly(9, 40));
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { alice });

        // N=3 but only 1 volunteer — should fill what it can, no error
        var act = async () => await _sut.AutoAssignShiftGroupAsync(tournament, "Pool", 3);

        await act.Should().NotThrowAsync();
        // Pool with start 09:00 and next phase Bracket at 09:40 produces 2 slots (09:00, 09:20)
        // given gameLength=20; alice fills each one once for a total of 2 Pool assignments.
        alice.Assignments.Where(a => a.ShiftGroup == "Pool").Should().HaveCount(2);
    }

    [Fact]
    public async Task AutoAssign_WorksForSpecialShiftGroup_FillsSingleSlotToN()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        var bob = Volunteer.Create("Bob", tournament.Id);
        var carol = Volunteer.Create("Carol", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { alice, bob, carol });

        await _sut.AutoAssignShiftGroupAsync(tournament, "Set-up", 2);

        var setupAssigned = new[] { alice, bob, carol }
            .Count(v => v.Assignments.Any(a => a.ShiftGroup == "Set-up" && a.ShiftTime is null));
        setupAssigned.Should().Be(2);
    }

    [Fact]
    public async Task AutoAssign_Throws_WhenShiftGroupUnknown()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(Array.Empty<Volunteer>());

        var act = async () => await _sut.AutoAssignShiftGroupAsync(tournament, "Unknown", 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- AutoAssign station colouring ---

    [Fact]
    public async Task AutoAssign_WithoutStations_LeavesVolunteersUncoloured()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        var bob = Volunteer.Create("Bob", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { alice, bob });

        await _sut.AutoAssignShiftGroupAsync(tournament, "Pool", 2, stationCount: null);

        new[] { alice, bob }.SelectMany(v => v.Assignments)
            .Should().OnlyContain(a => a.Station == null);
    }

    [Fact]
    public async Task AutoAssign_WithStations_SpreadsColoursUniformlyPerSlot()
    {
        var tournament = CreateTournament();
        // Single last phase => MaxRounds drives slot count; keep it to one slot for a clean spread.
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        var bob = Volunteer.Create("Bob", tournament.Id);
        var carol = Volunteer.Create("Carol", tournament.Id);
        var dave = Volunteer.Create("Dave", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { alice, bob, carol, dave });

        await _sut.AutoAssignShiftGroupAsync(tournament, "Pool", 4, stationCount: 2);

        // 4 volunteers across 2 stations, ordered by name (Alice,Bob,Carol,Dave) => 0,1,0,1.
        var slot = new TimeOnly(9, 0);
        alice.GetStation("Pool", slot).Should().Be(0);
        bob.GetStation("Pool", slot).Should().Be(1);
        carol.GetStation("Pool", slot).Should().Be(0);
        dave.GetStation("Pool", slot).Should().Be(1);
    }

    [Fact]
    public async Task AutoAssign_WithStations_OverwritesExistingManualColours()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        alice.AssignShift("Pool", new TimeOnly(9, 0));
        alice.SetStation("Pool", new TimeOnly(9, 0), 7); // pre-existing manual colour
        var bob = Volunteer.Create("Bob", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByTournamentIdAsync(tournament.Id))
            .Returns(new[] { alice, bob });

        await _sut.AutoAssignShiftGroupAsync(tournament, "Pool", 2, stationCount: 2);

        // Recoloured 0,1 by name order — Alice's manual 7 is overwritten.
        alice.GetStation("Pool", new TimeOnly(9, 0)).Should().Be(0);
        bob.GetStation("Pool", new TimeOnly(9, 0)).Should().Be(1);
    }

    // --- Manual assign-with-station upsert ---

    [Fact]
    public async Task AssignToShift_WithStation_SetsColour()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByIdAsync(alice.Id, tournament.Id)).Returns(alice);

        await _sut.AssignToShiftAsync(tournament, "Pool", "09:00", alice.Id, StationChange.Set(3));

        alice.GetStation("Pool", new TimeOnly(9, 0)).Should().Be(3);
    }

    [Fact]
    public async Task AssignToShift_BareReassign_PreservesExistingColour()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        alice.AssignShift("Pool", new TimeOnly(9, 0));
        alice.SetStation("Pool", new TimeOnly(9, 0), 5);
        A.CallTo(() => _volunteerRepository.GetByIdAsync(alice.Id, tournament.Id)).Returns(alice);

        await _sut.AssignToShiftAsync(tournament, "Pool", "09:00", alice.Id);

        alice.GetStation("Pool", new TimeOnly(9, 0)).Should().Be(5);
    }

    [Fact]
    public async Task AssignToShift_WithNullStationChange_ClearsExistingColour()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        alice.AssignShift("Pool", new TimeOnly(9, 0));
        alice.SetStation("Pool", new TimeOnly(9, 0), 5);
        A.CallTo(() => _volunteerRepository.GetByIdAsync(alice.Id, tournament.Id)).Returns(alice);

        await _sut.AssignToShiftAsync(tournament, "Pool", "09:00", alice.Id, StationChange.Set(null));

        alice.GetStation("Pool", new TimeOnly(9, 0)).Should().BeNull();
    }

    [Fact]
    public async Task AssignToShift_WithOutOfRangeStation_Throws()
    {
        var tournament = CreateTournament();
        var structure = CreateStructureWithPhase(tournament.Id);
        SetupStructureAndGames(structure);

        var alice = Volunteer.Create("Alice", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByIdAsync(alice.Id, tournament.Id)).Returns(alice);

        var act = async () =>
            // StationPalette.Count == 8 → indices 0..7 valid; 8 is the first out-of-range value.
            await _sut.AssignToShiftAsync(tournament, "Pool", "09:00", alice.Id, StationChange.Set(StationPalette.Count));

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task AssignToSpecialShift_WithStation_SetsColour()
    {
        var tournament = CreateTournament();

        var alice = Volunteer.Create("Alice", tournament.Id);
        A.CallTo(() => _volunteerRepository.GetByIdAsync(alice.Id, tournament.Id)).Returns(alice);

        await _sut.AssignToSpecialShiftAsync(tournament.Id, "Set-up", alice.Id, StationChange.Set(2));

        alice.GetStation("Set-up", null).Should().Be(2);
    }

    [Fact]
    public async Task AssignToSpecialShift_BareReassign_PreservesExistingColour()
    {
        var tournament = CreateTournament();

        var alice = Volunteer.Create("Alice", tournament.Id);
        alice.AssignShift("Set-up", null);
        alice.SetStation("Set-up", null, 6);
        A.CallTo(() => _volunteerRepository.GetByIdAsync(alice.Id, tournament.Id)).Returns(alice);

        await _sut.AssignToSpecialShiftAsync(tournament.Id, "Set-up", alice.Id);

        alice.GetStation("Set-up", null).Should().Be(6);
    }
}
