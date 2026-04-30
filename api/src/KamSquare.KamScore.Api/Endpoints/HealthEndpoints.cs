using KamSquare.KamScore.Application.Interfaces;

namespace KamSquare.KamScore.Api.Endpoints;

public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/health")
            .WithTags("Health")
            .RequireRateLimiting("public");

        group.MapGet("/", async (
            IDatabaseHealthProbe? probe,
            ILogger<HealthEndpointsLog> logger,
            CancellationToken ct) =>
        {
            if (probe is null)
            {
                logger.LogWarning(
                    "Health probe not registered; reporting healthy without DB check");
                return Results.Ok(new { status = "healthy" });
            }

            var reachable = await probe.IsReachableAsync(ct);
            return reachable
                ? Results.Ok(new { status = "healthy" })
                : Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Service Unavailable",
                    detail: "Database connectivity check failed");
        });

        return group;
    }

    // Marker type for ILogger<T> category — minimal-API endpoints have no
    // natural enclosing type for the logger to attach to.
    private sealed class HealthEndpointsLog;
}
