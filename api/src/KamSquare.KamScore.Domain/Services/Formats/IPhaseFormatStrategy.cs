using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public interface IPhaseFormatStrategy
{
    List<Game> GenerateGames(string tournamentId, string phaseId, string groupId, List<string> teamIds);
    List<Standing> CalculateStandings(List<Game> games, Group group);

    /// <summary>
    /// Rank standings across groups using the format's standings-criteria cascade only;
    /// group position is not considered. Used when picking the top-N qualifiers by overall
    /// performance regardless of which group they came from.
    /// </summary>
    List<Standing> RankCrossGroupByStats(List<Standing> standings);

    /// <summary>
    /// Rank standings across groups with group position as the primary key and the
    /// format's standings-criteria cascade as a tiebreaker within each position tier.
    /// Used when group winners are an explicit privileged class (both GroupWinners and
    /// TotalTeamsProceeding configured).
    /// </summary>
    List<Standing> RankCrossGroupByPosition(List<Standing> standings);

    bool SupportsRefereeAssignment { get; }
}
