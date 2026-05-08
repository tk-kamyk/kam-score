using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Domain.Services.Formats;

public static class PhaseFormatStrategy
{
    public static IPhaseFormatStrategy For(PhaseFormat format) => format switch
    {
        PhaseFormat.RoundRobin => new RoundRobinStrategy(),
        PhaseFormat.PlayoffElimination => new PlayoffEliminationStrategy(),
        PhaseFormat.PlayoffWithPlacement => new PlayoffWithPlacementStrategy(),
        PhaseFormat.DoubleElimination => new DoubleEliminationStrategy(),
        PhaseFormat.DoubleEliminationVd => new DoubleEliminationVdStrategy(),
        PhaseFormat.Custom => new CustomStrategy(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, $"Unsupported phase format: {format}")
    };

    public static IPhaseFormatStrategy For(PhaseFormat format, int teamCount) =>
        format == PhaseFormat.DoubleEliminationVd && teamCount != DoubleEliminationVdGenerator.RequiredTeamCount
            ? new DoubleEliminationStrategy()
            : For(format);
}
