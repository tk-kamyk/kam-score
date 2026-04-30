using System.Net;
using System.Net.Http.Json;
using FakeItEasy;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.Interfaces;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class HealthApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly KamScoreWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthApiTests(KamScoreWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        Fake.ClearRecordedCalls(_factory.FakeDatabaseHealthProbe);
        A.CallTo(() => _factory.FakeDatabaseHealthProbe.IsReachableAsync(A<CancellationToken>._))
            .Returns(true);
    }

    [Fact]
    public async Task Health_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ReturnsHealthy_WhenDatabaseProbeSucceeds()
    {
        A.CallTo(() => _factory.FakeDatabaseHealthProbe.IsReachableAsync(A<CancellationToken>._))
            .Returns(true);

        var response = await _client.GetAsync("/api/health");
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.Status.Should().Be("healthy");
        A.CallTo(() => _factory.FakeDatabaseHealthProbe.IsReachableAsync(A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Health_Returns503_WhenDatabaseProbeFails()
    {
        A.CallTo(() => _factory.FakeDatabaseHealthProbe.IsReachableAsync(A<CancellationToken>._))
            .Returns(false);

        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private sealed record HealthResponse(string Status);
}
