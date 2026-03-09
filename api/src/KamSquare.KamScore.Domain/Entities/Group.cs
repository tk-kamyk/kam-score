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

    public void AddTeam(string teamId) => TeamIds.Add(teamId);

    public bool RemoveTeam(string teamId) => TeamIds.Remove(teamId);

    public bool HasTeam(string teamId) => TeamIds.Contains(teamId);

    public void ClearTeams() => TeamIds.Clear();

    public void ReplaceTeamIds(Dictionary<string, string> mapping)
    {
        for (var i = 0; i < TeamIds.Count; i++)
        {
            if (mapping.TryGetValue(TeamIds[i], out var newId))
            {
                TeamIds[i] = newId;
            }
        }
    }
}
