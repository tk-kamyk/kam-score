using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Interfaces;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(string id, string tournamentId);
    Task<IEnumerable<Team>> GetByTournamentIdAsync(string tournamentId);
    Task<IEnumerable<Team>> GetBySourcePhaseIdAsync(string tournamentId, string sourcePhaseId);
    Task<Team> CreateAsync(Team team);
    Task<IEnumerable<Team>> CreateBatchAsync(IEnumerable<Team> teams);
    Task<Team> UpdateAsync(Team team);
    Task DeleteAsync(string id, string tournamentId);
    Task DeleteBySourcePhaseIdAsync(string tournamentId, string sourcePhaseId);
    Task DeleteByTournamentIdAsync(string tournamentId);
    Task<bool> ExistsByNameAsync(string tournamentId, string name, string? excludeTeamId = null);
    Task<int> CountByTournamentIdAsync(string tournamentId);
}
