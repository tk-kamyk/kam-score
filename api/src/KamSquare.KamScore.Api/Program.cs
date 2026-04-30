using System.Threading.RateLimiting;
using KamSquare.KamScore.Api.Endpoints;
using KamSquare.KamScore.Api.Filters;
using KamSquare.KamScore.Api.Middleware;
using KamSquare.KamScore.Infrastructure.DependencyInjection;
using KamSquare.KamScore.Infrastructure.Options;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure services (Cosmos, JWT auth, AutoMapper, FluentValidation, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// CORS
var corsOptions = builder.Configuration.GetSection("Cors").Get<CorsOptions>();
var allowedOrigins = corsOptions?.AllowedOrigins ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Rate limiting — per-client-IP fixed window so one attacker can't drain a global bucket.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => PerIpFixedWindow(httpContext, permitLimit: 10));
    options.AddPolicy("public", httpContext => PerIpFixedWindow(httpContext, permitLimit: 20));
});

static RateLimitPartition<string> PerIpFixedWindow(HttpContext httpContext, int permitLimit) =>
    RateLimitPartition.GetFixedWindowLimiter(
        ResolveClientPartitionKey(httpContext),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });

// Behind App Service / Container Apps the platform front end APPENDS the real
// client IP as the LAST X-Forwarded-For entry; earlier entries are
// attacker-supplied and ignored. We do not enable UseForwardedHeaders so the
// connection IP elsewhere remains the trusted edge IP.
static string ResolveClientPartitionKey(HttpContext httpContext)
{
    var xForwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
    if (!string.IsNullOrEmpty(xForwardedFor))
    {
        var lastComma = xForwardedFor.LastIndexOf(',');
        var lastEntry = lastComma >= 0 ? xForwardedFor[(lastComma + 1)..] : xForwardedFor;
        var trimmed = lastEntry.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            return trimmed;
        }
    }

    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "KamScore API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

// Swagger — before exception handling so errors aren't masked
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    await next();
});

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoint groups
app.MapAuthEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapTournamentEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapTeamEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapCourtEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapStructureEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapPhaseEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapGroupEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapLevelEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapTeamAssignmentEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapGameEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapStandingsEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapVolunteerEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapFeatureFlagEndpoints();
app.MapHealthEndpoints();

// Ensure Cosmos DB database and container exist
await app.Services.InitializeCosmosDbAsync();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
