using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Api.IntegrationTest;

// Own factory so this class's rate-limiter state is isolated from ResultApiTests.
//
// Per-test partition keys (each test owns a distinct rightmost IP so the three
// tests share the fixture without sharing a rate-limit bucket):
//   ExceedsPerWindowLimit                  → 203.0.113.50
//   DifferentClientIps_GetIndependentBuckets → 203.0.113.60 (exhausted) and 203.0.113.61 (fresh)
//   SpoofedLeftmostXForwardedFor           → 203.0.113.70 (rightmost, the real client IP)
//
// In-bucket requests assert NotBe(429) rather than a specific 4xx, so the test
// is independent of how the handler resolves missing auth/tournament-code (today
// FakeItEasy returns a dummy Tournament and the auth helper yields 401, but a
// future refactor that returns null → 404 would still satisfy NotBe(429)). What
// we are proving is "rate-limit middleware did or did not short-circuit", and
// any non-429 status proves it did not.
public class ScoreInputRateLimitTests : IClassFixture<ScoreInputRateLimitTests.IsolatedFactory>
{
    private const int PermitLimit = 20;

    private readonly HttpClient _client;

    public ScoreInputRateLimitTests(IsolatedFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RecordResult_ExceedsPerWindowLimit_Returns429()
    {
        const string clientIp = "203.0.113.50";

        for (var i = 0; i < PermitLimit; i++)
        {
            using var message = BuildRecordResultMessage(clientIp);
            var response = await _client.SendAsync(message);
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
                $"attempt {i + 1} should reach the endpoint pipeline, not be short-circuited by the rate limiter");
        }

        using var rejectedMessage = BuildRecordResultMessage(clientIp);
        var rejected = await _client.SendAsync(rejectedMessage);
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RecordResult_DifferentClientIps_GetIndependentBuckets()
    {
        for (var i = 0; i < PermitLimit; i++)
        {
            using var exhaust = BuildRecordResultMessage("203.0.113.60");
            (await _client.SendAsync(exhaust)).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        using var freshIp = BuildRecordResultMessage("203.0.113.61");
        var fresh = await _client.SendAsync(freshIp);
        fresh.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "a different client IP must not inherit another client's exhausted bucket");
    }

    [Fact]
    public async Task RecordResult_SpoofedLeftmostXForwardedFor_DoesNotBypassLimit()
    {
        const string realClientIp = "203.0.113.70";

        // Each request rotates the leftmost X-Forwarded-For entry while keeping the
        // rightmost (real client) IP fixed. If the limiter mistakenly partitioned on
        // the leftmost value, every request would hit a fresh bucket and the 21st
        // call below would return non-429 — failing this test rather than silently
        // passing. So this loop is genuinely falsifiable against a leftmost-keyed
        // implementation.
        for (var i = 0; i < PermitLimit; i++)
        {
            using var message = BuildRecordResultMessage($"1.2.3.{i}, {realClientIp}");
            (await _client.SendAsync(message)).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        using var rejectedMessage = BuildRecordResultMessage($"9.9.9.9, {realClientIp}");
        var rejected = await _client.SendAsync(rejectedMessage);
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "rotating the leftmost X-Forwarded-For value must not bypass per-IP throttling — only the rightmost (proxy-appended) entry counts");
    }

    private static HttpRequestMessage BuildRecordResultMessage(string xForwardedFor)
    {
        // Valid DTO so the ValidationFilter passes and the request reaches the handler
        // pipeline (where it ends in some non-429 status — exact code is intentionally
        // not asserted; see the class-level comment).
        var body = new GameResultDto(Sets: null, HomeScore: 2, AwayScore: 1);
        var message = new HttpRequestMessage(
            HttpMethod.Put,
            "/api/tournaments/missing-tournament/games/missing-game/result")
        {
            Content = JsonContent.Create(body),
        };
        message.Headers.Add("X-Forwarded-For", xForwardedFor);
        return message;
    }

    public sealed class IsolatedFactory : KamScoreWebApplicationFactory;
}
