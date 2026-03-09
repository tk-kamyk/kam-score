namespace KamSquare.KamScore.Application.DTOs;

public record LevelDto(
    string? Id,
    string Name,
    int? Order = null);
