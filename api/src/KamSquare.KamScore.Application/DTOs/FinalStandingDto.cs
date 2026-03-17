namespace KamSquare.KamScore.Application.DTOs;

public record FinalStandingDto(
    int Position,
    string TeamId,
    string TeamName,
    string? LevelName);
