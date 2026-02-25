namespace KamSquare.KamScore.Api.Endpoints;

public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/health")
            .WithTags("Health");

        group.MapGet("/", () => Results.Ok(new { status = "healthy" }));

        return group;
    }
}
