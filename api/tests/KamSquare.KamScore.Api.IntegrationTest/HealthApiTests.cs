using System.Net;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class HealthApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthApiTests(KamScoreWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
