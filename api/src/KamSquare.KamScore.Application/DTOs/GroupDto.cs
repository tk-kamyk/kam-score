namespace KamSquare.KamScore.Application.DTOs;

public record GroupDto(
    string? Id,
    string Name,
    List<string>? TeamIds = null);
