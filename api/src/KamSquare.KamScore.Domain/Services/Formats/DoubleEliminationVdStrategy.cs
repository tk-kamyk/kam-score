using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public class DoubleEliminationVdStrategy : IPhaseFormatStrategy
{
    public bool SupportsRefereeAssignment => false;

    public void ValidateTeams(List<Group> groups)
    {
        var invalidGroups = groups
            .Where(g => g.TeamIds.Count > 0 && g.TeamIds.Count != DoubleEliminationVdGenerator.RequiredTeamCount)
            .ToList();

        if (invalidGroups.Count > 0)
            throw new InvalidOperationException(
                $"Double Elimination (VD) requires exactly {DoubleEliminationVdGenerator.RequiredTeamCount} teams per group.");
    }

    public List<Game> GenerateGames(string tournamentId, string phaseId, string groupId, List<string> teamIds)
        => DoubleEliminationVdGenerator.Generate(tournamentId, phaseId, groupId, teamIds);

    public List<Standing> CalculateStandings(List<Game> games, List<string> teamIds)
        => DoubleEliminationVdStandingsRanker.Calculate(games, teamIds);

    public List<Standing> RankCrossGroup(List<Standing> standings)
        => DoubleEliminationVdStandingsRanker.RankCrossGroup(standings);
}
