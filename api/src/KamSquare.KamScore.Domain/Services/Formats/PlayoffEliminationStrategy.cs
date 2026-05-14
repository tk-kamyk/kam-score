using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public class PlayoffEliminationStrategy : IPhaseFormatStrategy
{
    public bool SupportsRefereeAssignment => false;

    public List<Game> GenerateGames(string tournamentId, string phaseId, string groupId, List<string> teamIds)
        => PlayoffEliminationGenerator.Generate(tournamentId, phaseId, groupId, teamIds);

    public List<Standing> CalculateStandings(List<Game> games, Group group)
        => PlayoffEliminationStandingsRanker.Calculate(games, group.TeamIds);

    public List<Standing> RankCrossGroupByStats(List<Standing> standings)
        => PlayoffEliminationStandingsRanker.RankCrossGroup(standings);

    public List<Standing> RankCrossGroupByPosition(List<Standing> standings)
        => PlayoffEliminationStandingsRanker.RankCrossGroup(standings);
}
