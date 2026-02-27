namespace KamSquare.KamScore.Application.DTOs;

public record GameDto(
    string? Id,
    string? PhaseId,
    string? GroupId,
    int? Round,
    string? HomeTeamId,
    string? AwayTeamId,
    string? HomeTeamPlaceholder,
    string? AwayTeamPlaceholder,
    string? RefereeTeamId,
    string? CourtId,
    string? StartTime,
    string? Status,
    string? HomeTeamName = null,
    string? AwayTeamName = null,
    string? RefereeTeamName = null,
    string? CourtName = null);
