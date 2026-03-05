using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Infrastructure.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace KamSquare.KamScore.Infrastructure.Persistence;

public class CosmosTournamentStructureRepository : CosmosRepository<TournamentStructure>, ITournamentStructureRepository
{
    public CosmosTournamentStructureRepository(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
        : base(cosmosClient, options)
    {
    }

    public async Task<TournamentStructure?> GetByTournamentIdAsync(string tournamentId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.tournamentId = @tournamentId")
            .WithParameter("@tournamentId", tournamentId);

        var results = await ExecuteQueryAsync<TournamentStructure>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
        return results.FirstOrDefault();
    }

    public async Task<TournamentStructure> CreateAsync(TournamentStructure structure)
    {
        var response = await Container.CreateItemAsync(
            structure,
            new PartitionKey(structure.TournamentId));

        return response.Resource;
    }

    public async Task<TournamentStructure> UpdateAsync(TournamentStructure structure)
    {
        var requestOptions = structure.ETag is not null
            ? new ItemRequestOptions { IfMatchEtag = structure.ETag }
            : null;
        var response = await Container.ReplaceItemAsync(
            structure,
            structure.Id,
            new PartitionKey(structure.TournamentId),
            requestOptions);
        var updated = response.Resource;
        SetETag(updated, response.ETag);
        return updated;
    }
}
