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
}
