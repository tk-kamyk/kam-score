using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Infrastructure.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace KamSquare.KamScore.Infrastructure.Persistence;

public class CosmosTournamentRepository : CosmosRepository<Tournament>, ITournamentRepository
{
    public CosmosTournamentRepository(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
        : base(cosmosClient, options)
    {
    }

    public async Task<Tournament?> GetByIdAsync(string id)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
            .WithParameter("@id", id);

        var results = await ExecuteQueryAsync<Tournament>(query);
        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<Tournament>> GetByOwnerIdAsync(string ownerId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.ownerId = @ownerId")
            .WithParameter("@ownerId", ownerId);

        return await ExecuteQueryAsync<Tournament>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(ownerId) });
    }

    public async Task<IEnumerable<Tournament>> GetAllAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c");
        return await ExecuteQueryAsync<Tournament>(query);
    }

    public async Task<Tournament> CreateAsync(Tournament tournament)
    {
        var response = await Container.CreateItemAsync(
            tournament,
            new PartitionKey(tournament.OwnerId));

        return response.Resource;
    }

    public async Task<Tournament> UpdateAsync(Tournament tournament)
    {
        var response = await Container.ReplaceItemAsync(
            tournament,
            tournament.Id,
            new PartitionKey(tournament.OwnerId));

        return response.Resource;
    }

    public async Task DeleteAsync(string id, string ownerId)
    {
        await Container.DeleteItemAsync<Tournament>(id, new PartitionKey(ownerId));
    }
}
