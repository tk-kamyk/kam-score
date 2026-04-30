using KamSquare.KamScore.Application.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace KamSquare.KamScore.Infrastructure.HealthChecks;

public class CosmosDbHealthProbe(
    CosmosClient cosmosClient,
    ILogger<CosmosDbHealthProbe> logger) : IDatabaseHealthProbe
{
    // Bounds the probe's latency so a stalled Cosmos call cannot pile up
    // unbounded RU-consuming requests when the database is degraded.
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

    private readonly CosmosClient _cosmosClient = cosmosClient;
    private readonly ILogger<CosmosDbHealthProbe> _logger = logger;

    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _cosmosClient.ReadAccountAsync().WaitAsync(ProbeTimeout, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cosmos DB health probe failed");
            return false;
        }
    }
}
