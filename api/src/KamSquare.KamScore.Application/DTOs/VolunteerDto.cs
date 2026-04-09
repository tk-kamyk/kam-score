namespace KamSquare.KamScore.Application.DTOs;

public record VolunteerDto(
    string? Id,
    string Name,
    string? Contact,
    string? TeamId,
    List<ShiftAssignmentDto>? Assignments = null);

public record ShiftAssignmentDto(
    string ShiftGroup,
    string? ShiftTime);
