using KamSquare.KamScore.Api.Endpoints;
using KamSquare.KamScore.Api.Filters;
using KamSquare.KamScore.Api.Middleware;
using KamSquare.KamScore.Infrastructure.DependencyInjection;
using KamSquare.KamScore.Infrastructure.Options;
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

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoint groups
app.MapAuthEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapTournamentEndpoints().AddEndpointFilter<ValidationFilter>();
app.MapHealthEndpoints();

// Ensure Cosmos DB database and container exist
await app.Services.InitializeCosmosDbAsync();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
