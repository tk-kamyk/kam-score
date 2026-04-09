using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Interfaces;

public interface IVolunteerRepository
{
    Task<Volunteer?> GetByIdAsync(string id, string tournamentId);
    Task<IEnumerable<Volunteer>> GetByTournamentIdAsync(string tournamentId);
    Task<Volunteer> CreateAsync(Volunteer volunteer);
    Task<Volunteer> UpdateAsync(Volunteer volunteer);
    Task DeleteAsync(string id, string tournamentId);
    Task DeleteByTournamentIdAsync(string tournamentId);
    Task<bool> ExistsByNameAsync(string tournamentId, string name, string? excludeVolunteerId = null);
    Task ClearTeamIdAsync(string tournamentId, string teamId);
}
