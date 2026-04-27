using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public interface IPhaseFormatStrategy
{
    List<Game> GenerateGames(string tournamentId, string phaseId, string groupId, List<string> teamIds);
    List<Standing> CalculateStandings(List<Game> games, Group group);
    List<Standing> RankCrossGroup(List<Standing> standings);
    bool SupportsRefereeAssignment { get; }
    void ValidateTeams(List<Group> groups);
}
