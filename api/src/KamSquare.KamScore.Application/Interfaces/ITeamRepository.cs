using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Interfaces;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(string id, string tournamentId);
    Task<IEnumerable<Team>> GetByTournamentIdAsync(string tournamentId);
    Task<Team> CreateAsync(Team team);
    Task<Team> UpdateAsync(Team team);
    Task DeleteAsync(string id, string tournamentId);
    Task<bool> ExistsByNameAsync(string tournamentId, string name, string? excludeTeamId = null);
}
