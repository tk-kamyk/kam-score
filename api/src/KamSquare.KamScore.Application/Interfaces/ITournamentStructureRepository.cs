using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Interfaces;

public interface ITournamentStructureRepository
{
    Task<TournamentStructure?> GetByTournamentIdAsync(string tournamentId);
    Task<TournamentStructure> CreateAsync(TournamentStructure structure);
    Task<TournamentStructure> UpdateAsync(TournamentStructure structure);
}
