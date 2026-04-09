using FluentAssertions;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Domain.UnitTest.Services;

public class VolunteerShiftCalculatorTests
{
    [Fact]
    public void CalculateShiftGroups_ShouldIncludeSetupAndCleanup()
    {
        var phases = new List<(string Name, TimeOnly? StartTime, int MaxRounds)>
        {
            ("Pool", new TimeOnly(9, 0), 3)
        };

        var result = VolunteerShiftCalculator.CalculateShiftGroups(phases, 20);

        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Set-up");
        result[0].IsSpecial.Should().BeTrue();
        result[^1].Name.Should().Be("Cleanup");
        result[^1].IsSpecial.Should().BeTrue();
    }

    [Fact]
    public void CalculateShiftGroups_SetupAndCleanup_ShouldHaveSingleNullShift()
    {
        var phases = new List<(string Name, TimeOnly? StartTime, int MaxRounds)>
        {
            ("Pool", new TimeOnly(9, 0), 3)
        };

        var result = VolunteerShiftCalculator.CalculateShiftGroups(phases, 20);

        result[0].Shifts.Should().HaveCount(1);
        result[0].Shifts[0].Should().BeNull();
        result[^1].Shifts.Should().HaveCount(1);
        result[^1].Shifts[0].Should().BeNull();
    }

    [Fact]
    public void CalculateShiftGroups_PhaseShifts_ShouldStartAtPhaseStartTime()
    {
        var phases = new List<(string Name, TimeOnly? StartTime, int MaxRounds)>
        {
            ("Pool", new TimeOnly(9, 0), 5)
        };

        var result = VolunteerShiftCalculator.CalculateShiftGroups(phases, 20);

        var poolGroup = result.First(g => g.Name == "Pool");
        poolGroup.Shifts[0].Should().Be(new TimeOnly(9, 0));
    }

    [Fact]
    public void CalculateShiftGroups_PhaseShifts_ShouldStepByGameLength()
    {
        var phases = new List<(string Name, TimeOnly? StartTime, int MaxRounds)>
        {
            ("Pool", new TimeOnly(9, 0), 5),
            ("Playoffs", new TimeOnly(11, 0), 3)
        };

        var result = VolunteerShiftCalculator.CalculateShiftGroups(phases, 20);

        var poolGroup = result.First(g => g.Name == "Pool");
        poolGroup.Shifts.Should().ContainInOrder(
            new TimeOnly(9, 0),
            new TimeOnly(9, 20),
            new TimeOnly(9, 40));
    }

    [Fact]
    public void CalculateShiftGroups_PhaseShifts_ShouldBeBoundedByNextPhaseStartTime()
    {
        var phases = new List<(string Name, TimeOnly? StartTime, int MaxRounds)>
        {
            ("Pool", new TimeOnly(10, 0), 10),
            ("Playoffs", new TimeOnly(11, 30), 3)
        };

        var result = VolunteerShiftCalculator.CalculateShiftGroups(phases, 20);

        var poolGroup = result.First(g => g.Name == "Pool");
        // 10:00, 10:20, 10:40, 11:00 = 4 shifts (11:20 would go past 11:30 boundary)
        poolGroup.Shifts.Should().HaveCount(4);
        poolGroup.Shifts.Should().ContainInOrder(
            new TimeOnly(10, 0),
            new TimeOnly(10, 20),
            new TimeOnly(10, 40),
            new TimeOnly(11, 0));
    }

    [Fact]
    public void CalculateShiftGroups_PartialSlots_ShouldBeDropped()
    {
        var phases = new List<(string Name, TimeOnly? StartTime, int MaxRounds)>
        {
            ("Pool", new TimeOnly(10, 0), 10),
            ("Playoffs", new TimeOnly(11, 30), 3)
        };

        var result = VolunteerShiftCalculator.CalculateShiftGroups(phases, 20);

        var poolGroup = result.First(g => g.Name == "Pool");
        poolGroup.Shifts.Should().NotContain(new TimeOnly(11, 20));
    }

    [Fact]
    public void CalculateShiftGroups_LastPhase_ShouldHaveShiftsEqualToMaxRounds()
    {
        var phases = new List<(string Name, TimeOnly? StartTime, int MaxRounds)>
        {
            ("Pool", new TimeOnly(9, 0), 3)
        };

        var result = VolunteerShiftCalculator.CalculateShiftGroups(phases, 20);

        var poolGroup = result.First(g => g.Name == "Pool");
        poolGroup.Shifts.Should().HaveCount(3);
    }

    [Fact]
    public void CalculateShiftGroups_PhaseWithoutStartTime_ShouldBeSingleNullShift()
    {
        var phases = new List<(string Name, TimeOnly? StartTime, int MaxRounds)>
        {
            ("Pool", null, 3)
        };

        var result = VolunteerShiftCalculator.CalculateShiftGroups(phases, 20);

        var poolGroup = result.First(g => g.Name == "Pool");
        poolGroup.IsSpecial.Should().BeFalse();
        poolGroup.Shifts.Should().HaveCount(1);
        poolGroup.Shifts[0].Should().BeNull();
    }

    [Fact]
    public void CalculateShiftGroups_NullGameLength_ShouldMakeAllPhasesSingleShifts()
    {
        var phases = new List<(string Name, TimeOnly? StartTime, int MaxRounds)>
        {
            ("Pool", new TimeOnly(9, 0), 3)
        };

        var result = VolunteerShiftCalculator.CalculateShiftGroups(phases, null);

        var poolGroup = result.First(g => g.Name == "Pool");
        poolGroup.Shifts.Should().HaveCount(1);
        poolGroup.Shifts[0].Should().BeNull();
    }

    [Fact]
    public void CalculateShiftGroups_MultiplePhases_ShouldReturnCorrectOrder()
    {
        var phases = new List<(string Name, TimeOnly? StartTime, int MaxRounds)>
        {
            ("Pool", new TimeOnly(9, 0), 3),
            ("Playoffs", new TimeOnly(11, 0), 2)
        };

        var result = VolunteerShiftCalculator.CalculateShiftGroups(phases, 20);

        result.Select(g => g.Name).Should().ContainInOrder("Set-up", "Pool", "Playoffs", "Cleanup");
    }
}
