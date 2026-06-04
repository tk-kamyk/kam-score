using System.Text.Json.Serialization;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Entities;

public class Volunteer : Entity
{
    private List<ShiftAssignment> _assignments = [];

    public string Name { get; set; } = null!;
    public string? Contact { get; set; }
    public string? TeamId { get; set; }
    public string TournamentId { get; set; } = null!;

    [JsonInclude]
    public IReadOnlyList<ShiftAssignment> Assignments
    {
        get => _assignments;
        private set => _assignments = value as List<ShiftAssignment> ?? value?.ToList() ?? [];
    }

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

    public void AssignShift(string shiftGroup, TimeOnly? shiftTime)
    {
        if (Find(shiftGroup, shiftTime) is not null) return;
        _assignments.Add(new ShiftAssignment(shiftGroup, shiftTime));
        LastModified = DateTime.UtcNow;
    }

    public void UnassignShift(string shiftGroup, TimeOnly? shiftTime)
    {
        var existing = Find(shiftGroup, shiftTime);
        if (existing is null) return;
        _assignments.Remove(existing);
        LastModified = DateTime.UtcNow;
    }

    public void SetStation(string shiftGroup, TimeOnly? shiftTime, int? station)
    {
        var existing = Find(shiftGroup, shiftTime);
        if (existing is null) return;
        existing.Station = station;
        LastModified = DateTime.UtcNow;
    }

    public int? GetStation(string shiftGroup, TimeOnly? shiftTime) =>
        Find(shiftGroup, shiftTime)?.Station;

    public bool IsAssignedTo(string shiftGroup, TimeOnly? shiftTime) =>
        Find(shiftGroup, shiftTime) is not null;

    private ShiftAssignment? Find(string shiftGroup, TimeOnly? shiftTime) =>
        _assignments.FirstOrDefault(a => a.ShiftGroup == shiftGroup && a.ShiftTime == shiftTime);
}
