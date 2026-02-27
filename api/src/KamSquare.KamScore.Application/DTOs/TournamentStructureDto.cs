namespace KamSquare.KamScore.Application.DTOs;

public record TournamentStructureDto(
    string? Id,
    string? TournamentId,
    List<PhaseDto>? Phases = null);
