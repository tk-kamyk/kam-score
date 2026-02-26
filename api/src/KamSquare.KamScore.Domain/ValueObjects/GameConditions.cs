namespace KamSquare.KamScore.Domain.ValueObjects;

public record GameConditions(int? BestOfSets = null, List<int>? PointsPerSet = null);
