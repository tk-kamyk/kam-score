namespace KamSquare.KamScore.Application.DTOs;

public record TeamDto(
    string? Id,
    string Name,
    int Level,
    string? Email,
    string? Phone);
