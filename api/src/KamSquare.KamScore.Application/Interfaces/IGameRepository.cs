using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Interfaces;

public interface IGameRepository
{
    Task<IEnumerable<Game>> GetByTournamentIdAsync(string tournamentId);
    Task<IEnumerable<Game>> GetByPhaseIdAsync(string tournamentId, string phaseId);
    Task<IEnumerable<Game>> CreateBatchAsync(IEnumerable<Game> games);
    Task DeleteByPhaseIdAsync(string tournamentId, string phaseId);
    Task<bool> GamesExistForPhaseAsync(string tournamentId, string phaseId);
}
