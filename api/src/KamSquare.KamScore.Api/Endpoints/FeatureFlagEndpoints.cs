using KamSquare.KamScore.Infrastructure.Options;

namespace KamSquare.KamScore.Api.Endpoints;

public static class FeatureFlagEndpoints
{
    public static RouteGroupBuilder MapFeatureFlagEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/feature-flags")
            .WithTags("FeatureFlags");

        group.MapGet("/", (IConfiguration configuration) =>
        {
            var flags = configuration.GetSection(FeatureFlagOptions.SectionName)
                .Get<Dictionary<string, bool>>() ?? [];
            return Results.Ok(flags);
        });

        return group;
    }
}
