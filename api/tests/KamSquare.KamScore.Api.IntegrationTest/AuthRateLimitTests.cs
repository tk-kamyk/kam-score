using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Api.IntegrationTest;

// Own factory so this class's rate-limiter state is isolated from AuthApiTests.
public class AuthRateLimitTests : IClassFixture<AuthRateLimitTests.IsolatedFactory>
{
    private readonly HttpClient _client;

    public AuthRateLimitTests(IsolatedFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ExceedsPerWindowLimit_Returns429()
    {
        var request = new LoginRequestDto("admin", "wrongpassword");
        const string clientIp = "203.0.113.10";

        for (var i = 0; i < 10; i++)
        {
            using var message = BuildLoginMessage(request, clientIp);
            var response = await _client.SendAsync(message);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"attempt {i + 1} should reach the endpoint and fail on credentials, not the rate limiter");
        }

        using var rejectedMessage = BuildLoginMessage(request, clientIp);
        var rejected = await _client.SendAsync(rejectedMessage);
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_DifferentClientIps_GetIndependentBuckets()
    {
        var request = new LoginRequestDto("admin", "wrongpassword");

        for (var i = 0; i < 10; i++)
        {
            using var exhaust = BuildLoginMessage(request, "203.0.113.20");
            (await _client.SendAsync(exhaust)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        using var freshIp = BuildLoginMessage(request, "203.0.113.21");
        var fresh = await _client.SendAsync(freshIp);
        fresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "a different client IP must not inherit another client's exhausted bucket");
    }

    [Fact]
    public async Task Login_SpoofedLeftmostXForwardedFor_DoesNotBypassLimit()
    {
        var request = new LoginRequestDto("admin", "wrongpassword");
        const string realClientIp = "203.0.113.30";

        for (var i = 0; i < 10; i++)
        {
            using var message = BuildLoginMessage(request, $"1.2.3.{i}, {realClientIp}");
            (await _client.SendAsync(message)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        using var rejectedMessage = BuildLoginMessage(request, $"9.9.9.9, {realClientIp}");
        var rejected = await _client.SendAsync(rejectedMessage);
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "rotating the leftmost X-Forwarded-For value must not bypass per-IP throttling — only the rightmost (proxy-appended) entry counts");
    }

    private static HttpRequestMessage BuildLoginMessage(LoginRequestDto request, string xForwardedFor)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(request),
        };
        message.Headers.Add("X-Forwarded-For", xForwardedFor);
        return message;
    }

    public sealed class IsolatedFactory : KamScoreWebApplicationFactory;
}
