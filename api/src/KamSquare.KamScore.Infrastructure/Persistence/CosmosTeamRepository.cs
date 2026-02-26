using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Infrastructure.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace KamSquare.KamScore.Infrastructure.Persistence;

public class CosmosTeamRepository : CosmosRepository<Team>, ITeamRepository
{
    public CosmosTeamRepository(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
        : base(cosmosClient, options)
    {
    }

    public async Task<Team?> GetByIdAsync(string id, string tournamentId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
            .WithParameter("@id", id);

        var iterator = Container.GetItemQueryIterator<Team>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });

        var results = new List<Team>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<Team>> GetByTournamentIdAsync(string tournamentId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.tournamentId = @tournamentId")
            .WithParameter("@tournamentId", tournamentId);

        var iterator = Container.GetItemQueryIterator<Team>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });

        var results = new List<Team>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<Team> CreateAsync(Team team)
    {
        var response = await Container.CreateItemAsync(
            team,
            new PartitionKey(team.TournamentId));

        return response.Resource;
    }

    public async Task<Team> UpdateAsync(Team team)
    {
        var response = await Container.ReplaceItemAsync(
            team,
            team.Id,
            new PartitionKey(team.TournamentId));

        return response.Resource;
    }

    public async Task DeleteAsync(string id, string tournamentId)
    {
        await Container.DeleteItemAsync<Team>(id, new PartitionKey(tournamentId));
    }

    public async Task<bool> ExistsByNameAsync(string tournamentId, string name, string? excludeTeamId = null)
    {
        var query = excludeTeamId is null
            ? new QueryDefinition(
                    "SELECT VALUE COUNT(1) FROM c WHERE c.tournamentId = @tournamentId AND LOWER(c.name) = LOWER(@name)")
                .WithParameter("@tournamentId", tournamentId)
                .WithParameter("@name", name)
            : new QueryDefinition(
                    "SELECT VALUE COUNT(1) FROM c WHERE c.tournamentId = @tournamentId AND LOWER(c.name) = LOWER(@name) AND c.id != @excludeId")
                .WithParameter("@tournamentId", tournamentId)
                .WithParameter("@name", name)
                .WithParameter("@excludeId", excludeTeamId);

        var iterator = Container.GetItemQueryIterator<int>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });

        var response = await iterator.ReadNextAsync();
        return response.Resource.FirstOrDefault() > 0;
    }
}
