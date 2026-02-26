using FakeItEasy;
using KamSquare.KamScore.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KamSquare.KamScore.Api.IntegrationTest.Infrastructure;

public class KamScoreWebApplicationFactory : WebApplicationFactory<Program>
{
    public ITournamentRepository FakeRepository { get; } = A.Fake<ITournamentRepository>();
    public ITeamRepository FakeTeamRepository { get; } = A.Fake<ITeamRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-that-is-at-least-32-characters-long!!",
                ["Jwt:Issuer"] = "KamScore",
                ["Jwt:Audience"] = "KamScore",
                ["Jwt:ExpirationMinutes"] = "480",
                ["Users:Entries:0:Username"] = "admin",
                ["Users:Entries:0:Password"] = "admin123",
                ["Users:Entries:0:DisplayName"] = "Administrator"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace ITournamentRepository with fake
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITournamentRepository));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            services.AddSingleton(FakeRepository);

            // Replace ITeamRepository with fake
            var teamDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITeamRepository));
            if (teamDescriptor != null)
            {
                services.Remove(teamDescriptor);
            }
            services.AddSingleton(FakeTeamRepository);

            // Replace authentication with test auth handler
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme, _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(string userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.TestUserIdHeader, userId);
        return client;
    }
}
