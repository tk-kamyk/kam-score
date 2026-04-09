namespace KamSquare.KamScore.Domain.Entities;

public class Volunteer : Entity
{
    public string Name { get; set; } = null!;
    public string? Contact { get; set; }
    public string? TeamId { get; set; }
    public string TournamentId { get; set; } = null!;

    public static Volunteer Create(string name, string tournamentId, string? contact = null, string? teamId = null)
    {
        throw new NotImplementedException();
    }

    public void Update(string name, string? contact, string? teamId)
    {
        throw new NotImplementedException();
    }
}
