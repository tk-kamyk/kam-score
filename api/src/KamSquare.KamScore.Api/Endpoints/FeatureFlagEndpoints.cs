using KamSquare.KamScore.Infrastructure.Options;

namespace KamSquare.KamScore.Api.Endpoints;

// Intentional boilerplate: no active flags today.
// The plumbing is retained so new in-development features can be gated behind
// a flag without re-introducing this infrastructure. See
// docs/requirements/feature-flags.md for usage.
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
