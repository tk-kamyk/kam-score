namespace KamSquare.KamScore.Application.DTOs;

public record UpdateManualStandingsDto(
    string PhaseId,
    string GroupId,
    List<string> OrderedTeamIds);
