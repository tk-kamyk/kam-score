namespace KamSquare.KamScore.Domain.ValueObjects;

public record GameConditions(int? WinningSets = null, List<int>? PointsPerSet = null);
