namespace KamSquare.KamScore.Application.DTOs;

public record PhaseDto(
    string? Id,
    string Name,
    string Format,
    int? Order = null,
    int? NumberOfGroups = null,
    int? GroupWinners = null,
    int? TotalTeamsProceeding = null,
    string? StartTime = null,
    List<GroupDto>? Groups = null);
