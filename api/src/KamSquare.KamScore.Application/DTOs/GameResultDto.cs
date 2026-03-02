namespace KamSquare.KamScore.Application.DTOs;

public record SetResultDto(int HomePoints, int AwayPoints);

public record GameResultDto(
    List<SetResultDto>? Sets,
    int? HomeScore,
    int? AwayScore);
