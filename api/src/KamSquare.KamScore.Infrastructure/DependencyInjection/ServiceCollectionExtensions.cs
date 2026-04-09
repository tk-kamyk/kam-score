using System.Text;
using FluentValidation;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Mappers;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Infrastructure.Options;
using KamSquare.KamScore.Infrastructure.Persistence;
using KamSquare.KamScore.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KamSquare.KamScore.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                o => !string.IsNullOrEmpty(o.Secret) && o.Secret.Length >= 32,
                "Jwt:Secret must be at least 32 characters.")
            .ValidateOnStart();
        services.AddOptions<UserOptions>()
            .Bind(configuration.GetSection(UserOptions.SectionName))
            .Validate(o => o.Entries.Count > 0, "At least one user entry is required.")
            .ValidateOnStart();
        services.Configure<CosmosDbOptions>(configuration.GetSection(CosmosDbOptions.SectionName));
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));

        // Cosmos DB
        var cosmosConnectionString = configuration.GetSection("CosmosDb:ConnectionString").Value;
        if (!string.IsNullOrEmpty(cosmosConnectionString))
        {
            services.AddSingleton(sp =>
            {
                var clientOptions = new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };
                return new CosmosClient(cosmosConnectionString, clientOptions);
            });
            services.AddScoped<ITournamentRepository, CosmosTournamentRepository>();
            services.AddScoped<ITeamRepository, CosmosTeamRepository>();
            services.AddScoped<ICourtRepository, CosmosCourtRepository>();
            services.AddScoped<ITournamentStructureRepository, CosmosTournamentStructureRepository>();
            services.AddScoped<IGameRepository, CosmosGameRepository>();
            services.AddScoped<IVolunteerRepository, CosmosVolunteerRepository>();
        }

        // Services
        services.AddSingleton<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<PhaseCompletionService>();
        services.AddScoped<PhaseGuardService>();
        services.AddScoped<ScheduleGenerationService>();
        services.AddHttpContextAccessor();

        // AutoMapper
        services.AddAutoMapper(typeof(TournamentProfile).Assembly);

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<TournamentProfile>();

        // JWT Authentication — configured via validated JwtOptions
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });
        services.AddAuthorization();

        return services;
    }

    public static async Task InitializeCosmosDbAsync(this IServiceProvider services)
    {
        var cosmosClient = services.GetService<CosmosClient>();
        if (cosmosClient is null) return;

        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("CosmosDbInitialization");
        var options = services.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(options.InitializationTimeoutSeconds));
        try
        {
            var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(
                options.DatabaseName, throughput: options.ProvisionedThroughput, cancellationToken: cts.Token);

            await database.Database.CreateContainerIfNotExistsAsync(
                CosmosRepository<Tournament>.GetContainerName(), "/ownerId", cancellationToken: cts.Token);
            await database.Database.CreateContainerIfNotExistsAsync(
                CosmosRepository<Team>.GetContainerName(), "/tournamentId", cancellationToken: cts.Token);
            await database.Database.CreateContainerIfNotExistsAsync(
                CosmosRepository<Court>.GetContainerName(), "/tournamentId", cancellationToken: cts.Token);
            await database.Database.CreateContainerIfNotExistsAsync(
                CosmosRepository<TournamentStructure>.GetContainerName(), "/tournamentId", cancellationToken: cts.Token);
            await database.Database.CreateContainerIfNotExistsAsync(
                CosmosRepository<Game>.GetContainerName(), "/tournamentId", cancellationToken: cts.Token);
            await database.Database.CreateContainerIfNotExistsAsync(
                CosmosRepository<Volunteer>.GetContainerName(), "/tournamentId", cancellationToken: cts.Token);

            logger.LogInformation("Cosmos DB initialized: {Database}", options.DatabaseName);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "Failed to initialize Cosmos DB within {Timeout} seconds", options.InitializationTimeoutSeconds);
            throw;
        }
    }
}
