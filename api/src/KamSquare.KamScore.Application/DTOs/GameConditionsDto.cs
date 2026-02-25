namespace KamSquare.KamScore.Application.DTOs;

public record GameConditionsDto(
    int? WinningSets,
    List<int>? PointsPerSet);
