using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public class PlayoffWithPlacementStrategy : IPhaseFormatStrategy
{
    public bool SupportsRefereeAssignment => false;

    public List<Game> GenerateGames(string tournamentId, string phaseId, string groupId, List<string> teamIds)
        => PlayoffWithPlacementGenerator.Generate(tournamentId, phaseId, groupId, teamIds);

    public List<Standing> CalculateStandings(List<Game> games, Group group)
        => PlayoffWithPlacementStandingsRanker.Calculate(games, group.TeamIds);

    public List<Standing> RankCrossGroup(List<Standing> standings)
        => PlayoffWithPlacementStandingsRanker.RankCrossGroup(standings);
}
