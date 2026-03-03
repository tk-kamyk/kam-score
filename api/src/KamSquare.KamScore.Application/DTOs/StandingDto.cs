namespace KamSquare.KamScore.Application.DTOs;

public record StandingDto(
    string TeamId,
    string? TeamName,
    int Position,
    int GamesPlayed,
    int Wins,
    int Draws,
    int Losses,
    int? Points,
    int? SetsWon,
    int? SetsLost,
    int? SetDifference,
    int? PointsWon,
    int? PointsLost,
    int? PointDifference);
