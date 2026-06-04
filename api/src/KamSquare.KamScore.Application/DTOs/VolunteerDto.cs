namespace KamSquare.KamScore.Application.DTOs;

public record VolunteerDto(
    string? Id,
    string Name,
    string? Contact,
    string? TeamId,
    List<ShiftAssignmentDto>? Assignments = null);

public record ShiftAssignmentDto(
    string ShiftGroup,
    string? ShiftTime,
    int? Station = null);

// Optional body on the assign endpoints. Body present (incl. Station = null) = set/clear the
// colour; body absent = bare assign that leaves any existing colour untouched.
public record AssignShiftRequestDto(int? Station);
