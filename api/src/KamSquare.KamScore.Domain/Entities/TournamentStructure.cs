using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;

namespace KamSquare.KamScore.Domain.Entities;

public class TournamentStructure : Entity
{
    public string TournamentId { get; set; } = null!;
    public List<Phase> Phases { get; set; } = [];

    public static TournamentStructure Create(string tournamentId)
    {
        return new TournamentStructure
        {
            Id = Guid.NewGuid().ToString(),
            TournamentId = tournamentId,
            LastModified = DateTime.UtcNow
        };
    }

    public Phase AddPhase(string name, PhaseFormat format, int numberOfGroups,
        int? groupWinners = null, int? totalTeamsProceeding = null, TimeOnly? startTime = null,
        int? numberOfLevels = null)
    {
        var previousPhase = Phases.Count > 0 ? Phases[^1] : null;
        ValidateLevelCount(numberOfLevels, previousPhase);

        var order = Phases.Count + 1;
        var phase = Phase.Create(name, format, order, numberOfGroups, groupWinners, totalTeamsProceeding, startTime, numberOfLevels);
        Phases.Add(phase);
        LastModified = DateTime.UtcNow;
        return phase;
    }

    public void UpdatePhase(string phaseId, string name, PhaseFormat format,
        int? groupWinners = null, int? totalTeamsProceeding = null, TimeOnly? startTime = null)
    {
        var phase = GetPhase(phaseId);
        phase.Update(name, format, groupWinners, totalTeamsProceeding, startTime);
        LastModified = DateTime.UtcNow;
    }

    public void RemovePhase(string phaseId)
    {
        var phase = GetPhase(phaseId);
        var previousPhase = Phases.FirstOrDefault(p => p.Order == phase.Order - 1);
        var nextPhase = Phases.FirstOrDefault(p => p.Order == phase.Order + 1);

        if (previousPhase is not null && nextPhase is not null)
        {
            var nextLevelCount = nextPhase.Levels.Count > 0 ? nextPhase.Levels.Count : (int?)null;
            ValidateLevelCount(nextLevelCount, previousPhase);
        }

        Phases.Remove(phase);
        ReorderPhases();
        LastModified = DateTime.UtcNow;
    }

    public Group AddGroup(string phaseId, string name)
    {
        var phase = GetPhase(phaseId);
        var group = Group.Create(name);
        phase.Groups.Add(group);
        LastModified = DateTime.UtcNow;
        return group;
    }

    public void UpdateGroup(string phaseId, string groupId, string name)
    {
        var group = GetGroup(phaseId, groupId);
        group.Update(name);
        LastModified = DateTime.UtcNow;
    }

    public void RemoveGroup(string phaseId, string groupId)
    {
        var phase = GetPhase(phaseId);
        var group = GetGroup(phaseId, groupId);
        phase.Groups.Remove(group);
        LastModified = DateTime.UtcNow;
    }

    public void AssignTeam(string phaseId, string groupId, string teamId)
    {
        var group = GetGroup(phaseId, groupId);
        group.AddTeam(teamId);
        LastModified = DateTime.UtcNow;
    }

    public void RemoveTeam(string phaseId, string groupId, string teamId)
    {
        var group = GetGroup(phaseId, groupId);

        if (!group.RemoveTeam(teamId))
            throw new NotFoundException("Team assignment", teamId);

        LastModified = DateTime.UtcNow;
    }

    public Phase GetPhase(string phaseId)
    {
        return Phases.FirstOrDefault(p => p.Id == phaseId)
            ?? throw new NotFoundException(nameof(Phase), phaseId);
    }

    public Group GetGroup(string phaseId, string groupId)
    {
        var phase = GetPhase(phaseId);
        return phase.Groups.FirstOrDefault(g => g.Id == groupId)
            ?? throw new NotFoundException(nameof(Group), groupId);
    }

    public Level GetLevel(string phaseId, string levelId)
    {
        var phase = GetPhase(phaseId);
        return phase.Levels.FirstOrDefault(l => l.Id == levelId)
            ?? throw new NotFoundException(nameof(Level), levelId);
    }

    public void UpdateLevel(string phaseId, string levelId, string name)
    {
        var level = GetLevel(phaseId, levelId);
        level.Update(name);
        LastModified = DateTime.UtcNow;
    }

    public bool LevelNameExistsInPhase(string phaseId, string name, string? excludeLevelId = null)
    {
        var phase = GetPhase(phaseId);
        return phase.Levels.Any(l =>
            l.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            (excludeLevelId is null || l.Id != excludeLevelId));
    }

    public List<Group> GetGroupsForLevel(string phaseId, string levelId)
    {
        var phase = GetPhase(phaseId);
        GetLevel(phaseId, levelId); // validate level exists
        return phase.Groups.Where(g => g.LevelId == levelId).ToList();
    }

    public bool GroupNameExistsInPhase(string phaseId, string name, string? excludeGroupId = null)
    {
        var phase = GetPhase(phaseId);
        return phase.Groups.Any(g =>
            g.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            (excludeGroupId is null || g.Id != excludeGroupId));
    }

    public bool TeamExistsInPhase(string phaseId, string teamId)
    {
        var phase = GetPhase(phaseId);
        return phase.Groups.Any(g => g.HasTeam(teamId));
    }

    public void SchedulePhase(string phaseId)
    {
        var phase = GetPhase(phaseId);
        phase.Schedule();
        LastModified = DateTime.UtcNow;
    }

    public void ActivatePhase(string phaseId)
    {
        var phase = GetPhase(phaseId);
        phase.Activate();
        LastModified = DateTime.UtcNow;
    }

    public void CompletePhase(string phaseId)
    {
        var phase = GetPhase(phaseId);
        phase.Complete();
        LastModified = DateTime.UtcNow;
    }

    public void ResetPhase(string phaseId)
    {
        var phase = GetPhase(phaseId);
        phase.Reset();
        LastModified = DateTime.UtcNow;
    }

    public void ReopenPhase(string phaseId)
    {
        var phase = GetPhase(phaseId);
        phase.Reopen();

        var nextPhase = GetNextPhase(phaseId);
        if (nextPhase is not null && nextPhase.Status == PhaseStatus.InProgress)
        {
            nextPhase.Schedule();
        }

        LastModified = DateTime.UtcNow;
    }

    public Phase? GetNextPhase(string phaseId)
    {
        var phase = GetPhase(phaseId);
        return Phases.FirstOrDefault(p => p.Order == phase.Order + 1);
    }

    public Phase? GetPreviousPhase(string phaseId)
    {
        var phase = GetPhase(phaseId);
        return Phases.FirstOrDefault(p => p.Order == phase.Order - 1);
    }

    public void AutoAssignTeams(string phaseId, List<Team> teams)
    {
        var orderedTeamIds = teams
            .OrderByDescending(t => t.Level)
            .ThenBy(t => t.Name)
            .Select(t => t.Id)
            .ToList();

        AutoAssignTeams(phaseId, orderedTeamIds);
    }

    public void AutoAssignTeams(string phaseId, List<string> orderedTeamIds, int sourceLevelCount = 0)
    {
        var phase = GetPhase(phaseId);

        foreach (var group in phase.Groups)
        {
            group.ClearTeams();
        }

        if (phase.Groups.Count == 0) return;

        if (phase.Levels.Count > 0)
        {
            AssignTeamsWithLevels(phase, orderedTeamIds, sourceLevelCount);
        }
        else
        {
            SnakeDraftIntoGroups(phase.Groups, orderedTeamIds);
        }

        LastModified = DateTime.UtcNow;
    }

    private static void AssignTeamsWithLevels(Phase phase, List<string> orderedTeamIds, int sourceLevelCount)
    {
        var orderedLevels = phase.Levels.OrderBy(l => l.Order).ToList();
        var targetLevelCount = orderedLevels.Count;

        // Level-scoped split: when target has more levels than source,
        // distribute each source level's teams across its child target levels
        if (sourceLevelCount > 0 && targetLevelCount > sourceLevelCount && targetLevelCount % sourceLevelCount == 0)
        {
            var splitFactor = targetLevelCount / sourceLevelCount;
            var teamsPerSourceLevel = orderedTeamIds.Count / sourceLevelCount;
            var sourceRemainder = orderedTeamIds.Count % sourceLevelCount;

            var offset = 0;
            for (var sourceLevelIndex = 0; sourceLevelIndex < sourceLevelCount; sourceLevelIndex++)
            {
                var sourceChunkSize = teamsPerSourceLevel + (sourceLevelIndex < sourceRemainder ? 1 : 0);
                var sourceChunk = orderedTeamIds.Skip(offset).Take(sourceChunkSize).ToList();
                offset += sourceChunkSize;

                var childLevels = orderedLevels.Skip(sourceLevelIndex * splitFactor).Take(splitFactor).ToList();
                DistributeAcrossLevels(phase, childLevels, sourceChunk);
            }

            return;
        }

        // Default: even distribution across all levels
        DistributeAcrossLevels(phase, orderedLevels, orderedTeamIds);
    }

    private static void DistributeAcrossLevels(Phase phase, List<Level> levels, List<string> teamIds)
    {
        var levelCount = levels.Count;
        var teamsPerLevel = teamIds.Count / levelCount;
        var remainder = teamIds.Count % levelCount;

        var offset = 0;
        for (var levelIndex = 0; levelIndex < levels.Count; levelIndex++)
        {
            var count = teamsPerLevel + (levelIndex < remainder ? 1 : 0);
            var chunk = teamIds.Skip(offset).Take(count).ToList();
            offset += count;

            var levelGroups = phase.Groups.Where(g => g.LevelId == levels[levelIndex].Id).ToList();
            SnakeDraftIntoGroups(levelGroups, chunk);
        }
    }

    private static void SnakeDraftIntoGroups(List<Group> groups, List<string> teamIds)
    {
        var groupCount = groups.Count;
        if (groupCount == 0) return;

        for (var i = 0; i < teamIds.Count; i++)
        {
            var round = i / groupCount;
            var positionInRound = i % groupCount;
            var groupIndex = round % 2 == 0 ? positionInRound : groupCount - 1 - positionInRound;
            groups[groupIndex].AddTeam(teamIds[i]);
        }
    }

    /// <summary>
    /// Validates that the new phase's level count is compatible with the previous phase.
    /// Rules:
    ///   - If previous phase has no levels, any count (including 0/null) is valid
    ///   - If previous phase has levels, new phase must also have levels and the count must be a multiple
    /// </summary>
    public static void ValidateLevelCount(int? numberOfLevels, Phase? previousPhase)
    {
        if (previousPhase is null || previousPhase.Levels.Count == 0)
            return;

        var previousLevelCount = previousPhase.Levels.Count;
        var newLevelCount = numberOfLevels is > 0 ? numberOfLevels.Value : 0;

        if (newLevelCount == 0)
            throw new ArgumentException(
                $"Phase must have levels because the previous phase has {previousLevelCount} levels.");

        if (newLevelCount < previousLevelCount)
            throw new ArgumentException(
                $"Phase must have at least {previousLevelCount} levels (same as the previous phase).");

        if (newLevelCount % previousLevelCount != 0)
            throw new ArgumentException(
                $"Number of levels ({newLevelCount}) must be a multiple of the previous phase's level count ({previousLevelCount}).");
    }

    private void ReorderPhases()
    {
        for (var i = 0; i < Phases.Count; i++)
        {
            Phases[i].Order = i + 1;
        }
    }
}
