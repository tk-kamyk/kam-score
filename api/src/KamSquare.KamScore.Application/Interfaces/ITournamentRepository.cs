using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Interfaces;

public interface ITournamentRepository
{
    Task<Tournament?> GetByIdAsync(string id);
    Task<IEnumerable<Tournament>> GetAllAsync();
    Task<Tournament> CreateAsync(Tournament tournament);
    Task<Tournament> UpdateAsync(Tournament tournament);
    Task DeleteAsync(string id, string ownerId);
}
