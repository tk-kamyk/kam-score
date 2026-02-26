using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Infrastructure.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace KamSquare.KamScore.Infrastructure.Persistence;

public class CosmosCourtRepository : CosmosRepository<Court>, ICourtRepository
{
    public CosmosCourtRepository(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
        : base(cosmosClient, options)
    {
    }

    public async Task<Court?> GetByIdAsync(string id, string tournamentId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
            .WithParameter("@id", id);

        var iterator = Container.GetItemQueryIterator<Court>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });

        var results = new List<Court>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<Court>> GetByTournamentIdAsync(string tournamentId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.tournamentId = @tournamentId")
            .WithParameter("@tournamentId", tournamentId);

        var iterator = Container.GetItemQueryIterator<Court>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });

        var results = new List<Court>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<Court> CreateAsync(Court court)
    {
        var response = await Container.CreateItemAsync(
            court,
            new PartitionKey(court.TournamentId));

        return response.Resource;
    }

    public async Task<Court> UpdateAsync(Court court)
    {
        var response = await Container.ReplaceItemAsync(
            court,
            court.Id,
            new PartitionKey(court.TournamentId));

        return response.Resource;
    }

    public async Task DeleteAsync(string id, string tournamentId)
    {
        await Container.DeleteItemAsync<Court>(id, new PartitionKey(tournamentId));
    }

    public async Task<bool> ExistsByNameAsync(string tournamentId, string name, string? excludeCourtId = null)
    {
        var query = excludeCourtId is null
            ? new QueryDefinition(
                    "SELECT VALUE COUNT(1) FROM c WHERE c.tournamentId = @tournamentId AND LOWER(c.name) = LOWER(@name)")
                .WithParameter("@tournamentId", tournamentId)
                .WithParameter("@name", name)
            : new QueryDefinition(
                    "SELECT VALUE COUNT(1) FROM c WHERE c.tournamentId = @tournamentId AND LOWER(c.name) = LOWER(@name) AND c.id != @excludeId")
                .WithParameter("@tournamentId", tournamentId)
                .WithParameter("@name", name)
                .WithParameter("@excludeId", excludeCourtId);

        var iterator = Container.GetItemQueryIterator<int>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });

        var response = await iterator.ReadNextAsync();
        return response.Resource.FirstOrDefault() > 0;
    }
}
