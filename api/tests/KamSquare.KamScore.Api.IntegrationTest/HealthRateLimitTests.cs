using System.Net;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;

namespace KamSquare.KamScore.Api.IntegrationTest;

// Own factory so this class's rate-limiter state is isolated from HealthApiTests.
public class HealthRateLimitTests : IClassFixture<HealthRateLimitTests.IsolatedFactory>
{
    private const int PermitLimit = 20;
    private readonly HttpClient _client;

    public HealthRateLimitTests(IsolatedFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ExceedsPerWindowLimit_Returns429()
    {
        const string clientIp = "203.0.113.40";

        for (var i = 0; i < PermitLimit; i++)
        {
            using var message = BuildHealthMessage(clientIp);
            var response = await _client.SendAsync(message);
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"attempt {i + 1} should be permitted under the public rate-limit policy");
        }

        using var rejectedMessage = BuildHealthMessage(clientIp);
        var rejected = await _client.SendAsync(rejectedMessage);
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private static HttpRequestMessage BuildHealthMessage(string xForwardedFor)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        message.Headers.Add("X-Forwarded-For", xForwardedFor);
        return message;
    }

    public sealed class IsolatedFactory : KamScoreWebApplicationFactory;
}
