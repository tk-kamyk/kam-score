using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Infrastructure.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace KamSquare.KamScore.Infrastructure.Persistence;

public class CosmosVolunteerRepository : CosmosRepository<Volunteer>, IVolunteerRepository
{
    public CosmosVolunteerRepository(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
        : base(cosmosClient, options)
    {
    }

    public Task<Volunteer?> GetByIdAsync(string id, string tournamentId)
        => throw new NotImplementedException();

    public Task<IEnumerable<Volunteer>> GetByTournamentIdAsync(string tournamentId)
        => throw new NotImplementedException();

    public Task<Volunteer> CreateAsync(Volunteer volunteer)
        => throw new NotImplementedException();

    public Task<Volunteer> UpdateAsync(Volunteer volunteer)
        => throw new NotImplementedException();

    public Task DeleteAsync(string id, string tournamentId)
        => throw new NotImplementedException();

    public Task DeleteByTournamentIdAsync(string tournamentId)
        => throw new NotImplementedException();

    public Task<bool> ExistsByNameAsync(string tournamentId, string name, string? excludeVolunteerId = null)
        => throw new NotImplementedException();

    public Task ClearTeamIdAsync(string tournamentId, string teamId)
        => throw new NotImplementedException();
}
