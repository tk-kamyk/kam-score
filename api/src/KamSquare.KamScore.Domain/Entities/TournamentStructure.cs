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
        int? groupWinners = null, int? totalTeamsProceeding = null, TimeOnly? startTime = null)
    {
        var order = Phases.Count + 1;
        var phase = Phase.Create(name, format, order, numberOfGroups, groupWinners, totalTeamsProceeding, startTime);
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
        Phases.Remove(phase);
        ReorderPhases();
        LastModified = DateTime.UtcNow;
    }

    public Group AddGroup(string phaseId, string name)
    {
        GetPhase(phaseId);
        var group = Group.Create(name);
        GetPhase(phaseId).Groups.Add(group);
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

    public void ReopenPhase(string phaseId)
    {
        var phase = GetPhase(phaseId);
        phase.Reopen();

        var nextPhase = GetNextPhase(phaseId);
        if (nextPhase is not null)
        {
            nextPhase.Status = PhaseStatus.New;
            foreach (var group in nextPhase.Groups)
            {
                group.ClearTeams();
            }
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
        var phase = GetPhase(phaseId);

        var orderedTeamIds = phase.Order == 1
            ? teams.OrderByDescending(t => t.Level).ThenBy(t => t.Name).Select(t => t.Id).ToList()
            : teams.OrderBy(_ => Random.Shared.Next()).Select(t => t.Id).ToList();

        AutoAssignTeams(phaseId, orderedTeamIds);
    }

    public void AutoAssignTeams(string phaseId, List<string> orderedTeamIds)
    {
        var phase = GetPhase(phaseId);

        foreach (var group in phase.Groups)
        {
            group.ClearTeams();
        }

        var groupCount = phase.Groups.Count;
        if (groupCount == 0) return;

        for (var i = 0; i < orderedTeamIds.Count; i++)
        {
            var round = i / groupCount;
            var positionInRound = i % groupCount;
            var groupIndex = round % 2 == 0 ? positionInRound : groupCount - 1 - positionInRound;
            phase.Groups[groupIndex].AddTeam(orderedTeamIds[i]);
        }

        LastModified = DateTime.UtcNow;
    }

    private void ReorderPhases()
    {
        for (var i = 0; i < Phases.Count; i++)
        {
            Phases[i].Order = i + 1;
        }
    }
}
