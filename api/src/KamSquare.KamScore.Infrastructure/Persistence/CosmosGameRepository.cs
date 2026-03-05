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
            var game = response.Resource;
            SetETag(game, response.ETag);
            return game;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Game> UpdateAsync(Game game)
    {
        var requestOptions = game.ETag is not null
            ? new ItemRequestOptions { IfMatchEtag = game.ETag }
            : null;
        var response = await Container.ReplaceItemAsync(
            game,
            game.Id,
            new PartitionKey(game.TournamentId),
            requestOptions);
        var updated = response.Resource;
        SetETag(updated, response.ETag);
        return updated;
    }

    public async Task<IEnumerable<Game>> CreateBatchAsync(IEnumerable<Game> games)
    {
        var tasks = games.Select(async game =>
        {
            var response = await Container.CreateItemAsync(
                game,
                new PartitionKey(game.TournamentId));
            return response.Resource;
        });
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    public async Task DeleteByPhaseIdAsync(string tournamentId, string phaseId)
    {
        var games = await GetByPhaseIdAsync(tournamentId, phaseId);
        await Task.WhenAll(games.Select(game =>
            Container.DeleteItemAsync<Game>(game.Id, new PartitionKey(tournamentId))));
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
