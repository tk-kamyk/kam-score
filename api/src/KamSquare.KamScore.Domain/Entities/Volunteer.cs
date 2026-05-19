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
        var assignment = new ShiftAssignment(shiftGroup, shiftTime);
        if (_assignments.Contains(assignment)) return;
        _assignments.Add(assignment);
        LastModified = DateTime.UtcNow;
    }

    public void UnassignShift(string shiftGroup, TimeOnly? shiftTime)
    {
        var assignment = new ShiftAssignment(shiftGroup, shiftTime);
        _assignments.Remove(assignment);
        LastModified = DateTime.UtcNow;
    }

    public bool IsAssignedTo(string shiftGroup, TimeOnly? shiftTime) =>
        _assignments.Any(a => a.ShiftGroup == shiftGroup && a.ShiftTime == shiftTime);
}
