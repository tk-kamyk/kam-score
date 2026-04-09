namespace KamSquare.KamScore.Application.DTOs;

public record VolunteerAvailabilityDto(
    string VolunteerId,
    string Name,
    int ShiftCount,
    bool Available,
    bool PlaysBefore,
    bool PlaysAfter,
    bool Assigned);
