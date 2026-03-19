using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services.Formats;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services;

public static class StandingsCalculator
{
    public static List<Standing> Calculate(PhaseFormat format, List<Game> games, List<string> teamIds)
    {
        var strategy = PhaseFormatStrategy.For(format);
        return strategy.CalculateStandings(games, teamIds);
    }
}
