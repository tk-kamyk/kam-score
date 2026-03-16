namespace KamSquare.KamScore.Application.DTOs;

public record FinalStandingDto(
    int Position,
    string TeamId,
    string TeamName,
    string? LevelName);

public record FinalStandingsResponseDto(
    bool Provisional,
    List<FinalStandingDto> Standings);
