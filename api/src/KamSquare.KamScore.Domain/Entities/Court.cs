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

    public static List<Court> GenerateCourts(int count, int startIndex, string tournamentId)
    {
        var courts = new List<Court>(count);
        for (var i = 0; i < count; i++)
        {
            courts.Add(Create($"C{startIndex + i}", tournamentId));
        }
        return courts;
    }
}
