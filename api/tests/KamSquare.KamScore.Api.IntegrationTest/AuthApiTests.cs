using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using KamSquare.KamScore.Api.IntegrationTest.Infrastructure;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Api.IntegrationTest;

public class AuthApiTests : IClassFixture<KamScoreWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthApiTests(KamScoreWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnToken()
    {
        var request = new LoginRequestDto("admin", "admin123");

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("admin");
        result.DisplayName.Should().Be("Administrator");
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShouldReturn401()
    {
        var request = new LoginRequestDto("admin", "wrongpassword");

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
