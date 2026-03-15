using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Interfaces;

public interface ICourtRepository
{
    Task<Court?> GetByIdAsync(string id, string tournamentId);
    Task<IEnumerable<Court>> GetByTournamentIdAsync(string tournamentId);
    Task<Court> CreateAsync(Court court);
    Task<Court> UpdateAsync(Court court);
    Task DeleteAsync(string id, string tournamentId);
    Task DeleteByTournamentIdAsync(string tournamentId);
    Task<bool> ExistsByNameAsync(string tournamentId, string name, string? excludeCourtId = null);
    Task<int> CountByTournamentIdAsync(string tournamentId);
}
