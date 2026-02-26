namespace KamSquare.KamScore.Domain.Entities;

public class Court : Entity
{
    public string Name { get; set; } = null!;
    public string TournamentId { get; set; } = null!;

    public static Court Create(string name, string tournamentId)
    {
        return new Court
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            TournamentId = tournamentId,
            LastModified = DateTime.UtcNow
        };
    }

    public void Update(string name)
    {
        Name = name;
        LastModified = DateTime.UtcNow;
    }
}
