using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Infrastructure.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace KamSquare.KamScore.Infrastructure.Persistence;

public abstract class CosmosRepository<T> where T : Entity
{
    protected readonly Container Container;

    protected CosmosRepository(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
    {
        var db = cosmosClient.GetDatabase(options.Value.DatabaseName);
        Container = db.GetContainer(GetContainerName());
    }

    public static string GetContainerName()
        => typeof(T).Name.ToLowerInvariant() + "s";

    protected async Task<List<TResult>> ExecuteQueryAsync<TResult>(
        QueryDefinition query,
        QueryRequestOptions? requestOptions = null)
    {
        var iterator = Container.GetItemQueryIterator<TResult>(query, requestOptions: requestOptions);
        var results = new List<TResult>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    protected static void SetETag(Entity entity, string? etag)
    {
        entity.ETag = etag;
    }
}
