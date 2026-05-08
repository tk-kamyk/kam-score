using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public class RoundRobinStrategy : IPhaseFormatStrategy
{
    public bool SupportsRefereeAssignment => true;

    public List<Game> GenerateGames(string tournamentId, string phaseId, string groupId, List<string> teamIds)
        => RoundRobinGenerator.Generate(tournamentId, phaseId, groupId, teamIds);

    public List<Standing> CalculateStandings(List<Game> games, Group group)
        => RoundRobinStandingsRanker.Calculate(games, group.TeamIds);

    public List<Standing> RankCrossGroup(List<Standing> standings)
        => RoundRobinStandingsRanker.RankCrossGroup(standings);
}
