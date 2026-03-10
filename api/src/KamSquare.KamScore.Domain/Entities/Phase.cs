using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Domain.Entities;

/// <summary>
/// Nested value object within TournamentStructure aggregate — not an independent entity.
/// </summary>
public class Phase
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PhaseFormat Format { get; set; }
    public int Order { get; set; }
    public int? GroupWinners { get; set; }
    public int? TotalTeamsProceeding { get; set; }
    public TimeOnly? StartTime { get; set; }
    public PhaseStatus Status { get; set; } = PhaseStatus.New;
    public List<Level> Levels { get; set; } = [];
    public List<Group> Groups { get; set; } = [];

    public static Phase Create(string name, PhaseFormat format, int order, int numberOfGroups,
        int? groupWinners = null, int? totalTeamsProceeding = null, TimeOnly? startTime = null,
        int? numberOfLevels = null)
    {
        var phase = new Phase
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Format = format,
            Order = order,
            GroupWinners = groupWinners,
            TotalTeamsProceeding = totalTeamsProceeding,
            StartTime = startTime
        };

        if (numberOfLevels is not > 0)
        {
            for (var i = 0; i < numberOfGroups; i++)
            {
                phase.Groups.Add(Group.Create(GetGroupName(i)));
            }

            return phase;
        }

        for (var l = 0; l < numberOfLevels; l++)
        {
            phase.Levels.Add(Level.Create($"Level {l + 1}", l + 1));
        }

        var groupIndex = 0;
        foreach (var level in phase.Levels)
        {
            for (var g = 0; g < numberOfGroups; g++)
            {
                phase.Groups.Add(Group.Create(GetGroupName(groupIndex), level.Id));
                groupIndex++;
            }
        }

        return phase;
    }

    public void Update(string name, PhaseFormat format, int? groupWinners, int? totalTeamsProceeding,
        TimeOnly? startTime)
    {
        Name = name;
        Format = format;
        GroupWinners = groupWinners;
        TotalTeamsProceeding = totalTeamsProceeding;
        StartTime = startTime;
    }

    public bool HasStructuralChanges(PhaseFormat newFormat, TimeOnly? newStartTime)
    {
        return Format != newFormat || StartTime != newStartTime;
    }

    public void Activate()
    {
        Status = PhaseStatus.InProgress;
    }

    public void Complete()
    {
        Status = PhaseStatus.Completed;
    }

    public void Reopen()
    {
        Status = PhaseStatus.InProgress;
    }

    public void Reset()
    {
        Status = PhaseStatus.New;
    }

    private static string GetGroupName(int index)
    {
        return ((char)('A' + index)).ToString();
    }
}
