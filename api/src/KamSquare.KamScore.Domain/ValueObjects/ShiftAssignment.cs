namespace KamSquare.KamScore.Domain.ValueObjects;

// Identity is (ShiftGroup, ShiftTime). Station is a mutable attribute (an optional colour
// index, null = none) that is deliberately EXCLUDED from equality — equality and hashing
// use the identity pair only, so changing a colour never creates or hides an assignment and
// the type is safe in hashed collections. See docs/design/volunteer.md.
public record ShiftAssignment(string ShiftGroup, TimeOnly? ShiftTime)
{
    public int? Station { get; set; }

    public virtual bool Equals(ShiftAssignment? other) =>
        other is not null && ShiftGroup == other.ShiftGroup && ShiftTime == other.ShiftTime;

    public override int GetHashCode() => HashCode.Combine(ShiftGroup, ShiftTime);
}
