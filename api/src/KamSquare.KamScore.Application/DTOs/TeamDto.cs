namespace KamSquare.KamScore.Application.DTOs;

public record TeamDto(
    string? Id,
    string Name,
    int Level,
    string? Email,
    string? Phone,
    bool IsPlaceholder = false,
    string? SourcePhaseId = null,
    int? Seed = null,
    string? ResolvedTeamId = null);
