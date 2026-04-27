namespace KamSquare.KamScore.Domain.Entities;

/// <summary>
/// Nested value object within TournamentStructure aggregate — not an independent entity.
/// </summary>
public class Group
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LevelId { get; set; }
    public List<string> TeamIds { get; set; } = [];

    /// <summary>
    /// Owner-entered final standings ordering for Custom-format phases.
    /// Index i = position i+1. Empty when no ordering has been saved or
    /// when the phase format is not Custom.
    /// </summary>
    public List<string> ManualStandingOrder { get; set; } = [];

    public static Group Create(string name, string? levelId = null)
    {
        return new Group
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            LevelId = levelId
        };
    }

    public void Update(string name)
    {
        Name = name;
    }

    public void AddTeam(string teamId)
    {
        TeamIds.Add(teamId);
        ClearManualStandingOrder();
    }

    public bool RemoveTeam(string teamId)
    {
        var removed = TeamIds.Remove(teamId);
        if (removed) ClearManualStandingOrder();
        return removed;
    }

    public bool HasTeam(string teamId) => TeamIds.Contains(teamId);

    public void ClearTeams()
    {
        TeamIds.Clear();
        ClearManualStandingOrder();
    }

    public void ReplaceTeamIds(Dictionary<string, string> mapping)
    {
        for (var i = 0; i < TeamIds.Count; i++)
        {
            if (mapping.TryGetValue(TeamIds[i], out var newId))
            {
                TeamIds[i] = newId;
            }
        }
        ClearManualStandingOrder();
    }

    public void SetManualStandingOrder(IReadOnlyList<string> orderedTeamIds)
    {
        if (orderedTeamIds.Count != TeamIds.Count)
            throw new ArgumentException(
                $"Manual standings must cover every team in the group: expected {TeamIds.Count} entries, got {orderedTeamIds.Count}.",
                nameof(orderedTeamIds));

        if (orderedTeamIds.Distinct().Count() != orderedTeamIds.Count)
            throw new ArgumentException(
                "Manual standings cannot contain duplicate team IDs.",
                nameof(orderedTeamIds));

        if (orderedTeamIds.Any(id => !TeamIds.Contains(id)))
            throw new ArgumentException(
                "Manual standings can only reference teams assigned to this group.",
                nameof(orderedTeamIds));

        ManualStandingOrder = orderedTeamIds.ToList();
    }

    public void ClearManualStandingOrder() => ManualStandingOrder.Clear();
}
