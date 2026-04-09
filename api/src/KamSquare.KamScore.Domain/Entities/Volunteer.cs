namespace KamSquare.KamScore.Domain.Entities;

public class Volunteer : Entity
{
    public string Name { get; set; } = null!;
    public string? Contact { get; set; }
    public string? TeamId { get; set; }
    public string TournamentId { get; set; } = null!;

    public static Volunteer Create(string name, string tournamentId, string? contact = null, string? teamId = null)
    {
        return new Volunteer
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            TournamentId = tournamentId,
            Contact = contact,
            TeamId = teamId,
            LastModified = DateTime.UtcNow
        };
    }

    public void Update(string name, string? contact, string? teamId)
    {
        Name = name;
        Contact = contact;
        TeamId = teamId;
        LastModified = DateTime.UtcNow;
    }
}
