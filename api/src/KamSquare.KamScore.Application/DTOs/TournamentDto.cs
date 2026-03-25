namespace KamSquare.KamScore.Application.DTOs;

public record TournamentDto(
    string? Id,
    string Name,
    string Discipline,
    DateTime? StartTime,
    int? GameLength,
    GameConditionsDto? GameConditions,
    string? TournamentCode,
    string? OwnerId,
    DateTime? LastModified = null,
    int? TeamCount = null,
    int? CourtCount = null);
