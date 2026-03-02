using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Infrastructure.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace KamSquare.KamScore.Infrastructure.Persistence;

public class CosmosGameRepository : CosmosRepository<Game>, IGameRepository
{
    public CosmosGameRepository(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
        : base(cosmosClient, options)
    {
    }

    public async Task<IEnumerable<Game>> GetByTournamentIdAsync(string tournamentId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.tournamentId = @tournamentId")
            .WithParameter("@tournamentId", tournamentId);

        return await ExecuteQueryAsync<Game>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
    }

    public async Task<IEnumerable<Game>> GetByPhaseIdAsync(string tournamentId, string phaseId)
    {
        var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.tournamentId = @tournamentId AND c.phaseId = @phaseId")
            .WithParameter("@tournamentId", tournamentId)
            .WithParameter("@phaseId", phaseId);

        return await ExecuteQueryAsync<Game>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
    }

    public async Task<Game?> GetByIdAsync(string tournamentId, string gameId)
    {
        try
        {
            var response = await Container.ReadItemAsync<Game>(
                gameId,
                new PartitionKey(tournamentId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Game> UpdateAsync(Game game)
    {
        var response = await Container.UpsertItemAsync(game, new PartitionKey(game.TournamentId));
        return response.Resource;
    }

    public async Task<IEnumerable<Game>> CreateBatchAsync(IEnumerable<Game> games)
    {
        var created = new List<Game>();
        foreach (var game in games)
        {
            var response = await Container.CreateItemAsync(
                game,
                new PartitionKey(game.TournamentId));
            created.Add(response.Resource);
        }
        return created;
    }

    public async Task DeleteByPhaseIdAsync(string tournamentId, string phaseId)
    {
        var games = await GetByPhaseIdAsync(tournamentId, phaseId);
        foreach (var game in games)
        {
            await Container.DeleteItemAsync<Game>(game.Id, new PartitionKey(tournamentId));
        }
    }

    public async Task<bool> GamesExistForPhaseAsync(string tournamentId, string phaseId)
    {
        var query = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.tournamentId = @tournamentId AND c.phaseId = @phaseId")
            .WithParameter("@tournamentId", tournamentId)
            .WithParameter("@phaseId", phaseId);

        var results = await ExecuteQueryAsync<int>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
        return results.FirstOrDefault() > 0;
    }
}
