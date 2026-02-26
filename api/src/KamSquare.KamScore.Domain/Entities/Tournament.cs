using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Entities;

public class Tournament : Entity
{
    public string Name { get; set; } = null!;
    public Discipline Discipline { get; set; }
    public DateTime? StartTime { get; set; }
    public int? GameLength { get; set; }
    public GameConditions? GameConditions { get; set; }
    public string TournamentCode { get; set; } = null!;
    public string OwnerId { get; set; } = null!;

    public List<Court> Courts { get; set; } = [];
    public List<Phase> Phases { get; set; } = [];

    public static Tournament Create(string name, Discipline discipline, string ownerId)
    {
        return new Tournament
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Discipline = discipline,
            OwnerId = ownerId,
            TournamentCode = GenerateTournamentCode(),
            LastModified = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        Discipline discipline,
        DateTime? startTime,
        int? gameLength,
        GameConditions? gameConditions)
    {
        Name = name;
        Discipline = discipline;
        StartTime = startTime;
        GameLength = gameLength;
        GameConditions = gameConditions;
        LastModified = DateTime.UtcNow;
    }

    public bool IsOwnedBy(string userId)
    {
        return OwnerId == userId;
    }

    public static string GenerateTournamentCode()
    {
        return Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
    }
}
