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
    public ICourtRepository FakeCourtRepository { get; } = A.Fake<ICourtRepository>();
    public ITournamentStructureRepository FakeStructureRepository { get; } = A.Fake<ITournamentStructureRepository>();
    public IGameRepository FakeGameRepository { get; } = A.Fake<IGameRepository>();
    public IVolunteerRepository FakeVolunteerRepository { get; } = A.Fake<IVolunteerRepository>();
    public IDatabaseHealthProbe FakeDatabaseHealthProbe { get; } = CreateHealthyProbe();

    private static IDatabaseHealthProbe CreateHealthyProbe()
    {
        var probe = A.Fake<IDatabaseHealthProbe>();
        A.CallTo(() => probe.IsReachableAsync(A<CancellationToken>._)).Returns(true);
        return probe;
    }

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

            // Replace ICourtRepository with fake
            var courtDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ICourtRepository));
            if (courtDescriptor != null)
            {
                services.Remove(courtDescriptor);
            }
            services.AddSingleton(FakeCourtRepository);

            // Replace ITournamentStructureRepository with fake
            var structureDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITournamentStructureRepository));
            if (structureDescriptor != null)
            {
                services.Remove(structureDescriptor);
            }
            services.AddSingleton(FakeStructureRepository);

            // Replace IGameRepository with fake
            var gameDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IGameRepository));
            if (gameDescriptor != null)
            {
                services.Remove(gameDescriptor);
            }
            services.AddSingleton(FakeGameRepository);

            // Replace IVolunteerRepository with fake
            var volunteerDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IVolunteerRepository));
            if (volunteerDescriptor != null)
            {
                services.Remove(volunteerDescriptor);
            }
            services.AddSingleton(FakeVolunteerRepository);

            // Replace IDatabaseHealthProbe with fake (defaults to "reachable")
            var probeDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDatabaseHealthProbe));
            if (probeDescriptor != null)
            {
                services.Remove(probeDescriptor);
            }
            services.AddSingleton(FakeDatabaseHealthProbe);

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

    public HttpClient CreateAdminClient(string userId)
    {
        var client = CreateAuthenticatedClient(userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.TestRoleHeader, "Admin");
        return client;
    }
}
