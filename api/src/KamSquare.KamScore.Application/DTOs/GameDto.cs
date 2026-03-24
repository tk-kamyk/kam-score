namespace KamSquare.KamScore.Application.DTOs;

public record GameDto(
    string? Id,
    string? PhaseId,
    string? GroupId,
    int? Round,
    string? Label,
    string? HomeTeamId,
    string? AwayTeamId,
    string? HomeTeamPlaceholder,
    string? AwayTeamPlaceholder,
    string? RefereeTeamId,
    string? RefereeTeamPlaceholder,
    string? CourtId,
    string? StartTime,
    string? Status,
    int? HomeScore = null,
    int? AwayScore = null,
    List<SetResultDto>? Sets = null,
    string? HomeTeamName = null,
    string? AwayTeamName = null,
    string? RefereeTeamName = null,
    string? CourtName = null,
    bool? HomeTeamIsPlaceholder = null,
    bool? AwayTeamIsPlaceholder = null,
    string? PhaseName = null,
    string? GroupName = null,
    string? LevelName = null);

public record AssignRefereeDto(string? TeamId = null, string? Placeholder = null);

public record RefereeCandidateDto(string TeamId, string TeamName, bool IsPlaceholder = false);
