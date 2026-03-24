using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class FeatureFlagApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;

    public FeatureFlagApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetFeatureFlags_WithNoFlagsConfigured_ReturnsEmptyObject()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/feature-flags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var flags = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
        flags.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetFeatureFlags_WithConfiguredFlags_ReturnsFlags()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureFlags:LiveScoring"] = "true",
                    ["FeatureFlags:Referees"] = "false"
                });
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/feature-flags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var flags = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
        flags.Should().NotBeNull();
        flags!["LiveScoring"].Should().BeTrue();
        flags["Referees"].Should().BeFalse();
    }

    [Fact]
    public async Task GetFeatureFlags_WithoutAuthentication_ReturnsJsonResponse()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/feature-flags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
