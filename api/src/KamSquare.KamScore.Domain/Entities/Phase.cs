using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services.Formats;
using KamSquare.KamScore.Domain.ValueObjects;

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
        var formatChanged = Format != format;

        Name = name;
        Format = format;
        GroupWinners = groupWinners;
        TotalTeamsProceeding = totalTeamsProceeding;
        StartTime = startTime;

        if (formatChanged)
        {
            foreach (var group in Groups)
                group.ClearManualStandingOrder();
        }
    }

    public bool HasProgressionConfig => GroupWinners is not null || TotalTeamsProceeding is not null;

    public bool SupportsRefereeAssignment => PhaseFormatStrategy.For(Format).SupportsRefereeAssignment;

    public bool HasStructuralChanges(PhaseFormat newFormat, TimeOnly? newStartTime)
    {
        return Format != newFormat || StartTime != newStartTime;
    }

    public void Schedule()
    {
        if (Status is not (PhaseStatus.New or PhaseStatus.InProgress))
            throw new PhaseStateException(Name, "schedule", $"phase must be New or InProgress, but is {Status}");
        Status = PhaseStatus.Scheduled;
    }

    public void Activate()
    {
        if (Status is not (PhaseStatus.New or PhaseStatus.Scheduled))
            throw new PhaseStateException(Name, "activate", $"phase must be New or Scheduled, but is {Status}");
        Status = PhaseStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != PhaseStatus.InProgress)
            throw new PhaseStateException(Name, "complete", $"phase must be InProgress, but is {Status}");
        Status = PhaseStatus.Completed;
    }

    public void Reopen()
    {
        if (Status != PhaseStatus.Completed)
            throw new PhaseStateException(Name, "reopen", $"phase must be Completed, but is {Status}");
        Status = PhaseStatus.InProgress;
    }

    public void Reset()
    {
        if (Status is not (PhaseStatus.Scheduled or PhaseStatus.InProgress))
            throw new PhaseStateException(Name, "reset", $"phase must be Scheduled or InProgress, but is {Status}");
        Status = PhaseStatus.New;
    }

    public List<Game> GenerateGames(string tournamentId)
    {
        var strategy = PhaseFormatStrategy.For(Format);
        strategy.ValidateTeams(Groups);

        var allGames = new List<Game>();
        foreach (var group in Groups)
        {
            if (group.TeamIds.Count <= 1) continue;
            allGames.AddRange(strategy.GenerateGames(tournamentId, Id, group.Id, group.TeamIds));
        }

        return allGames;
    }

    public List<Standing> CalculateGroupStandings(string groupId, List<Game> groupGames)
    {
        var group = Groups.FirstOrDefault(g => g.Id == groupId)
            ?? throw new NotFoundException(nameof(Group), groupId);
        var strategy = PhaseFormatStrategy.For(Format);
        return strategy.CalculateStandings(groupGames, group);
    }

    public List<(string GroupId, List<Standing> Standings)> CalculateAllGroupStandings(List<Game> phaseGames)
    {
        var strategy = PhaseFormatStrategy.For(Format);
        return Groups
            .Select(g =>
            {
                var groupGames = phaseGames.Where(game => game.GroupId == g.Id).ToList();
                return (g.Id, strategy.CalculateStandings(groupGames, g));
            })
            .ToList();
    }

    private static string GetGroupName(int index)
    {
        return ((char)('A' + index)).ToString();
    }
}
