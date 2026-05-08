using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

/// <summary>
/// Phase format for matches played outside the system. Does not generate games;
/// standings are derived from <see cref="Group.ManualStandingOrder"/> entered by
/// the owner.
/// </summary>
public class CustomStrategy : IPhaseFormatStrategy
{
    public bool SupportsRefereeAssignment => false;

    public List<Game> GenerateGames(string tournamentId, string phaseId, string groupId, List<string> teamIds)
        => CustomGenerator.Generate();

    public List<Standing> CalculateStandings(List<Game> games, Group group)
        => CustomStandingsRanker.Calculate(group);

    public List<Standing> RankCrossGroup(List<Standing> standings)
        => CustomStandingsRanker.RankCrossGroup(standings);
}
