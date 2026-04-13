using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public class PlayoffWithPlacementStrategy : IPhaseFormatStrategy
{
    public bool SupportsRefereeAssignment => false;

    public void ValidateTeams(List<Group> groups)
    {
        // No format-specific team count constraints
    }

    public List<Game> GenerateGames(string tournamentId, string phaseId, string groupId, List<string> teamIds)
        => PlayoffWithPlacementGenerator.Generate(tournamentId, phaseId, groupId, teamIds);

    public List<Standing> CalculateStandings(List<Game> games, List<string> teamIds)
        => PlayoffWithPlacementStandingsRanker.Calculate(games, teamIds);

    public List<Standing> RankCrossGroup(List<Standing> standings)
        => PlayoffWithPlacementStandingsRanker.RankCrossGroup(standings);
}
