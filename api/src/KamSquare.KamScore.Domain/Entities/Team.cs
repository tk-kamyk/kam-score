namespace KamSquare.KamScore.Domain.Entities;

public class Team : Entity
{
    private const int MaxLevel = 100;
    private const int DefaultSingleTeamLevel = 50;
    public string Name { get; set; } = null!;
    public int Level { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string TournamentId { get; set; } = null!;
    public bool IsPlaceholder { get; set; }
    public string? SourcePhaseId { get; set; }
    public int? Seed { get; set; }
    public string? ResolvedTeamId { get; set; }

    /// <summary>
    /// Returns ResolvedTeamId if the placeholder has been resolved, otherwise the placeholder's own Id.
    /// </summary>
    public string EffectiveId => ResolvedTeamId ?? Id;

    public static Team Create(string name, int level, string tournamentId, string? email = null, string? phone = null)
    {
        return new Team
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Level = level,
            TournamentId = tournamentId,
            Email = email,
            Phone = phone,
            LastModified = DateTime.UtcNow
        };
    }

    public static Team CreatePlaceholder(string name, string tournamentId, string sourcePhaseId, int seed)
    {
        return new Team
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            TournamentId = tournamentId,
            IsPlaceholder = true,
            SourcePhaseId = sourcePhaseId,
            Seed = seed,
            LastModified = DateTime.UtcNow
        };
    }

    public void Update(string name, int level, string? email, string? phone)
    {
        Name = name;
        Level = level;
        Email = email;
        Phone = phone;
        LastModified = DateTime.UtcNow;
    }

    public static List<Team> GenerateSeedTeams(int count, int startIndex, string tournamentId)
    {
        var teams = new List<Team>(count);
        for (var i = 0; i < count; i++)
        {
            var level = count == 1 ? DefaultSingleTeamLevel : MaxLevel - (int)Math.Round((double)i * MaxLevel / (count - 1));
            teams.Add(Create($"Seed {startIndex + i}", level, tournamentId));
        }
        return teams;
    }
}
