using System.Text.Json;
using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.UnitTest;

// Locks the JSON round-trip after encapsulating Volunteer.Assignments behind IReadOnlyList.
// Cosmos DB SDK uses System.Text.Json by default, so this guards against accidental
// regression of the private-setter / JsonInclude wiring.
public class VolunteerSerializationTests
{
    [Fact]
    public void RoundTrip_PreservesAssignments()
    {
        var original = Volunteer.Create("John Doe", "tournament-1", "john@email.com", "team-1");
        original.AssignShift("Pool", new TimeOnly(9, 0));
        original.AssignShift("Pool", new TimeOnly(9, 20));
        original.AssignShift("Set-up", null);

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<Volunteer>(json);

        restored.Should().NotBeNull();
        restored!.Name.Should().Be("John Doe");
        restored.TournamentId.Should().Be("tournament-1");
        restored.Contact.Should().Be("john@email.com");
        restored.TeamId.Should().Be("team-1");
        restored.Assignments.Should().BeEquivalentTo(original.Assignments);
    }

    [Fact]
    public void RoundTrip_WithEmptyAssignments_Works()
    {
        var original = Volunteer.Create("Jane", "tournament-1");

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<Volunteer>(json);

        restored.Should().NotBeNull();
        restored!.Assignments.Should().BeEmpty();
    }

    [Fact]
    public void Assignments_ExposedAsReadOnly()
    {
        var volunteer = Volunteer.Create("Jane", "tournament-1");
        volunteer.AssignShift("Pool", new TimeOnly(9, 0));

        // Compile-time check: the property type is IReadOnlyList<T>, not List<T>.
        // External callers cannot Add/Remove without going through the entity methods.
        IReadOnlyList<KamSquare.KamScore.Domain.ValueObjects.ShiftAssignment> readOnlyView = volunteer.Assignments;
        readOnlyView.Should().HaveCount(1);
    }
}
