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

        var results = await ExecuteQueryAsync<Team>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<Team>> GetByTournamentIdAsync(string tournamentId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.tournamentId = @tournamentId")
            .WithParameter("@tournamentId", tournamentId);

        return await ExecuteQueryAsync<Team>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
    }

    public async Task<IEnumerable<Team>> GetBySourcePhaseIdAsync(string tournamentId, string sourcePhaseId)
    {
        var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.tournamentId = @tournamentId AND c.sourcePhaseId = @sourcePhaseId")
            .WithParameter("@tournamentId", tournamentId)
            .WithParameter("@sourcePhaseId", sourcePhaseId);

        return await ExecuteQueryAsync<Team>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
    }

    public async Task<Team> CreateAsync(Team team)
    {
        var response = await Container.CreateItemAsync(
            team,
            new PartitionKey(team.TournamentId));

        return response.Resource;
    }

    public async Task<IEnumerable<Team>> CreateBatchAsync(IEnumerable<Team> teams)
    {
        var tasks = teams.Select(async team =>
        {
            var response = await Container.CreateItemAsync(
                team,
                new PartitionKey(team.TournamentId));
            return response.Resource;
        });
        var results = await Task.WhenAll(tasks);
        return results.ToList();
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

    public async Task DeleteBySourcePhaseIdAsync(string tournamentId, string sourcePhaseId)
    {
        var teams = await GetBySourcePhaseIdAsync(tournamentId, sourcePhaseId);
        await Task.WhenAll(teams.Select(team =>
            Container.DeleteItemAsync<Team>(team.Id, new PartitionKey(tournamentId))));
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

        var results = await ExecuteQueryAsync<int>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
        return results.FirstOrDefault() > 0;
    }

    public async Task<int> CountByTournamentIdAsync(string tournamentId)
    {
        var query = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.tournamentId = @tournamentId")
            .WithParameter("@tournamentId", tournamentId);

        var results = await ExecuteQueryAsync<int>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
        return results.FirstOrDefault();
    }
}
