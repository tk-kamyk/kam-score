namespace KamSquare.KamScore.Domain.ValueObjects;

public record FinalStanding(
    int Position,
    string TeamId,
    string TeamName,
    string? LevelName);
