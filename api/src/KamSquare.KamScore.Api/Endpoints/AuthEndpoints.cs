using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;

namespace KamSquare.KamScore.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/login", Login);

        return group;
    }

    private static IResult Login(LoginRequestDto request, IAuthService authService)
    {
        var result = authService.Authenticate(request.Username, request.Password);

        if (result is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new LoginResponseDto(result.Token, result.Username, result.DisplayName, result.Role));
    }
}
