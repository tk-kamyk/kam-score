namespace KamSquare.KamScore.Application.DTOs;

public record GameConditionsDto(
    int? BestOfSets,
    List<int>? PointsPerSet);
