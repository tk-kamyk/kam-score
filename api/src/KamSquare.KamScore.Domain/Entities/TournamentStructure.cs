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

    public Phase AddPhase(string name, PhaseFormat format, int numberOfGroups)
    {
        var order = Phases.Count + 1;
        var phase = Phase.Create(name, format, order, numberOfGroups);
        Phases.Add(phase);
        LastModified = DateTime.UtcNow;
        return phase;
    }

    public void UpdatePhase(string phaseId, string name, PhaseFormat format)
    {
        var phase = GetPhase(phaseId);
        phase.Update(name, format);
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
        group.TeamIds.Add(teamId);
        LastModified = DateTime.UtcNow;
    }

    public void RemoveTeam(string phaseId, string groupId, string teamId)
    {
        var group = GetGroup(phaseId, groupId);

        if (!group.TeamIds.Remove(teamId))
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
        return phase.Groups.Any(g => g.TeamIds.Contains(teamId));
    }

    private void ReorderPhases()
    {
        for (var i = 0; i < Phases.Count; i++)
        {
            Phases[i].Order = i + 1;
        }
    }
}
