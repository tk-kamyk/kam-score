namespace KamSquare.KamScore.Domain.Services;

public record ShiftGroup(string Name, List<TimeOnly?> Shifts, bool IsSpecial);

public static class VolunteerShiftCalculator
{
    public static List<ShiftGroup> CalculateShiftGroups(
        List<(string Name, TimeOnly? StartTime, int MaxRounds)> phases,
        int? gameLengthMinutes)
    {
        var result = new List<ShiftGroup>
        {
            new("Set-up", [null], true)
        };

        for (var i = 0; i < phases.Count; i++)
        {
            var phase = phases[i];
            var shifts = CalculatePhaseShifts(phase, i < phases.Count - 1 ? phases[i + 1].StartTime : null, gameLengthMinutes);
            result.Add(new ShiftGroup(phase.Name, shifts, false));
        }

        result.Add(new ShiftGroup("Cleanup", [null], true));

        return result;
    }

    private static List<TimeOnly?> CalculatePhaseShifts(
        (string Name, TimeOnly? StartTime, int MaxRounds) phase,
        TimeOnly? nextPhaseStartTime,
        int? gameLengthMinutes)
    {
        if (phase.StartTime is null || gameLengthMinutes is null or <= 0)
            return [null];

        var start = phase.StartTime.Value;
        var step = gameLengthMinutes.Value;
        var shifts = new List<TimeOnly?>();

        if (nextPhaseStartTime is not null)
        {
            var current = start;
            var safetyLimit = 100;
            while (current.AddMinutes(step) <= nextPhaseStartTime.Value && shifts.Count < safetyLimit)
            {
                shifts.Add(current);
                current = current.AddMinutes(step);
            }
        }
        else
        {
            for (var round = 0; round < phase.MaxRounds; round++)
            {
                shifts.Add(start.AddMinutes(round * step));
            }
        }

        if (shifts.Count == 0)
            return [null];

        return shifts;
    }
}
