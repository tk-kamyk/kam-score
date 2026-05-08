using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public class DoubleEliminationStrategy : IPhaseFormatStrategy
{
    public bool SupportsRefereeAssignment => false;

    public List<Game> GenerateGames(string tournamentId, string phaseId, string groupId, List<string> teamIds)
        => DoubleEliminationGenerator.Generate(tournamentId, phaseId, groupId, teamIds);

    public List<Standing> CalculateStandings(List<Game> games, Group group)
        => DoubleEliminationStandingsRanker.Calculate(games, group.TeamIds);

    public List<Standing> RankCrossGroup(List<Standing> standings)
        => DoubleEliminationStandingsRanker.RankCrossGroup(standings);
}
