using System.Text.RegularExpressions;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Entities;

public partial class Tournament : Entity
{
    public string Name { get; set; } = null!;
    public Discipline Discipline { get; set; }
    public DateTime? StartTime { get; set; }
    public int? GameLength { get; set; }
    public GameConditions? GameConditions { get; set; }
    public string TournamentCode { get; set; } = null!;
    public string OwnerId { get; set; } = null!;

    public List<Court> Courts { get; set; } = [];

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

    [GeneratedRegex("^[0-9A-Fa-f]{4}$")]
    private static partial Regex TournamentCodeRegex();

    public bool IsCodeValid(string? code)
    {
        if (string.IsNullOrEmpty(code))
            return false;
        if (!TournamentCodeRegex().IsMatch(code))
            return false;
        return TournamentCode.Equals(code, StringComparison.OrdinalIgnoreCase);
    }

    public static string GenerateTournamentCode()
    {
        return Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
    }
}
