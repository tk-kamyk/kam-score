namespace KamSquare.KamScore.Domain.ValueObjects;

public record Standing(
    string TeamId,
    int Position,
    int GamesPlayed,
    int Wins,
    int Draws,
    int Losses,
    int? Points,
    int? SetsWon,
    int? SetsLost,
    int? SetDifference);
