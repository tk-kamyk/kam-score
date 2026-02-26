namespace KamSquare.KamScore.Domain.Entities;

public class Team : Entity
{
    public string Name { get; set; } = null!;
    public int Level { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string TournamentId { get; set; } = null!;

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

    public void Update(string name, int level, string? email, string? phone)
    {
        Name = name;
        Level = level;
        Email = email;
        Phone = phone;
        LastModified = DateTime.UtcNow;
    }
}
