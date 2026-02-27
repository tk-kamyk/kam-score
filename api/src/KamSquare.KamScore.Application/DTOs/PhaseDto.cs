namespace KamSquare.KamScore.Application.DTOs;

public record PhaseDto(
    string? Id,
    string Name,
    string Format,
    int? Order = null,
    int? NumberOfGroups = null,
    List<GroupDto>? Groups = null);
