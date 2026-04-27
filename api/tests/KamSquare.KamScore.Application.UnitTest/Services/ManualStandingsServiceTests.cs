using FakeItEasy;
using FluentAssertions;
using FluentValidation;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;

namespace KamSquare.KamScore.Application.UnitTest.Services;

public class ManualStandingsServiceTests
{
    private const string TournamentId = "tournament-1";
    private const string PhaseId = "phase-1";
    private const string GroupId = "group-1";

    private readonly ITournamentStructureRepository _structureRepository =
        A.Fake<ITournamentStructureRepository>();
    private readonly ManualStandingsService _sut;

    public ManualStandingsServiceTests()
    {
        _sut = new ManualStandingsService(_structureRepository);
    }

    private static TournamentStructure CreateCustomStructure(
        PhaseStatus status = PhaseStatus.InProgress,
        params string[] teamIds)
    {
        var structure = TournamentStructure.Create(TournamentId);
        var phase = structure.AddPhase("Custom", PhaseFormat.Custom, numberOfGroups: 1);
        phase.Id = PhaseId;
        phase.Groups[0].Id = GroupId;
        foreach (var id in teamIds) phase.Groups[0].AddTeam(id);

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
    public async Task Update_StoresOrdering_AndReturnsStandings()
    {
        var structure = CreateCustomStructure(teamIds: ["t1", "t2", "t3"]);

        var standings = await _sut.UpdateAsync(
            TournamentId, PhaseId, GroupId, ["t2", "t3", "t1"], structure);

        standings.Should().HaveCount(3);
        standings.Single(s => s.TeamId == "t2").Position.Should().Be(1);
        structure.Phases[0].Groups[0].ManualStandingOrder
            .Should().BeEquivalentTo(["t2", "t3", "t1"], opts => opts.WithStrictOrdering());
        A.CallTo(() => _structureRepository.UpdateAsync(structure)).MustHaveHappened();
    }

    [Fact]
    public async Task Update_RejectsNonCustomFormat()
    {
        var structure = TournamentStructure.Create(TournamentId);
        var phase = structure.AddPhase("Pool", PhaseFormat.RoundRobin, 1);
        phase.Id = PhaseId;
        phase.Groups[0].Id = GroupId;
        phase.Groups[0].AddTeam("t1");
        phase.Groups[0].AddTeam("t2");
        phase.Activate();

        var act = async () => await _sut.UpdateAsync(
            TournamentId, PhaseId, GroupId, ["t1", "t2"], structure);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Format"));
    }

    [Fact]
    public async Task Update_RejectsWhenPhaseIsNotInProgress()
    {
        var structure = CreateCustomStructure(PhaseStatus.New, teamIds: ["t1", "t2"]);

        var act = async () => await _sut.UpdateAsync(
            TournamentId, PhaseId, GroupId, ["t1", "t2"], structure);

        await act.Should().ThrowAsync<PhaseStateException>();
    }

    [Fact]
    public async Task Update_PropagatesDomainValidation_OnInvalidOrdering()
    {
        var structure = CreateCustomStructure(teamIds: ["t1", "t2", "t3"]);

        var act = async () => await _sut.UpdateAsync(
            TournamentId, PhaseId, GroupId, ["t1", "t-other", "t3"], structure);

        await act.Should().ThrowAsync<ValidationException>();
        A.CallTo(() => _structureRepository.UpdateAsync(A<TournamentStructure>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Update_Throws_NotFound_ForUnknownGroup()
    {
        var structure = CreateCustomStructure(teamIds: ["t1", "t2"]);

        var act = async () => await _sut.UpdateAsync(
            TournamentId, PhaseId, "unknown-group", ["t1", "t2"], structure);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
