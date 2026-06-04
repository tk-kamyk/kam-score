namespace KamSquare.KamScore.Application.DTOs;

public record ShiftGroupResponseDto(
    string Name,
    bool IsSpecial,
    List<ShiftSlotDto> Shifts);

public record ShiftSlotDto(
    string? ShiftTime,
    List<ShiftVolunteerDto> Volunteers);

public record ShiftVolunteerDto(
    string VolunteerId,
    string Name,
    bool Available,
    int? Station = null);
