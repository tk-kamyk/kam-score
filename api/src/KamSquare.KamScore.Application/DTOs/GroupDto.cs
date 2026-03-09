namespace KamSquare.KamScore.Application.DTOs;

public record GroupDto(
    string? Id,
    string Name,
    string? LevelId = null,
    List<string>? TeamIds = null);
