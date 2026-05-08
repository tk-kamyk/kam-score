using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public class DoubleEliminationVdStrategy : IPhaseFormatStrategy
{
    private readonly DoubleEliminationStrategy _standardFallback = new();

    public bool SupportsRefereeAssignment => false;

    public List<Game> GenerateGames(string tournamentId, string phaseId, string groupId, List<string> teamIds)
    {
        if (teamIds.Count == DoubleEliminationVdGenerator.RequiredTeamCount)
            return DoubleEliminationVdGenerator.Generate(tournamentId, phaseId, groupId, teamIds);

        return _standardFallback.GenerateGames(tournamentId, phaseId, groupId, teamIds);
    }

    public List<Standing> CalculateStandings(List<Game> games, Group group)
    {
        if (group.TeamIds.Count == DoubleEliminationVdGenerator.RequiredTeamCount)
            return DoubleEliminationVdStandingsRanker.Calculate(games, group.TeamIds);

        return _standardFallback.CalculateStandings(games, group);
    }

    public List<Standing> RankCrossGroup(List<Standing> standings)
        => DoubleEliminationVdStandingsRanker.RankCrossGroup(standings);
}
