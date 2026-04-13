using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public class RoundRobinStrategy : IPhaseFormatStrategy
{
    public bool SupportsRefereeAssignment => true;

    public void ValidateTeams(List<Group> groups)
    {
        // No format-specific team count constraints for round robin
    }

    public List<Game> GenerateGames(string tournamentId, string phaseId, string groupId, List<string> teamIds)
        => RoundRobinGenerator.Generate(tournamentId, phaseId, groupId, teamIds);

    public List<Standing> CalculateStandings(List<Game> games, List<string> teamIds)
        => RoundRobinStandingsRanker.Calculate(games, teamIds);

    public List<Standing> RankCrossGroup(List<Standing> standings)
        => RoundRobinStandingsRanker.RankCrossGroup(standings);
}
