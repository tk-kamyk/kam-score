using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Interfaces;

public interface IGameRepository
{
    Task<IEnumerable<Game>> GetByTournamentIdAsync(string tournamentId);
    Task<IEnumerable<Game>> GetByPhaseIdAsync(string tournamentId, string phaseId);
    Task<Game?> GetByIdAsync(string tournamentId, string gameId);
    Task<IEnumerable<Game>> CreateBatchAsync(IEnumerable<Game> games);
    Task<Game> UpdateAsync(Game game);
    Task DeleteByPhaseIdAsync(string tournamentId, string phaseId);
    Task<bool> GamesExistForPhaseAsync(string tournamentId, string phaseId);
}
