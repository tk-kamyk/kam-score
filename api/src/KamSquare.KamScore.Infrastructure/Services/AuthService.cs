using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KamSquare.KamScore.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly JwtOptions _jwtOptions;
    private readonly UserOptions _userOptions;

    public AuthService(IOptions<JwtOptions> jwtOptions, IOptions<UserOptions> userOptions)
    {
        _jwtOptions = jwtOptions.Value;
        _userOptions = userOptions.Value;
    }

    public AuthResult? Authenticate(string username, string password)
    {
        var user = _userOptions.Entries
            .FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);

        if (user is null)
        {
            return null;
        }

        var token = GenerateToken(user);
        return new AuthResult(token, user.Username, user.DisplayName);
    }

    private string GenerateToken(UserEntry user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Username),
            new Claim(ClaimTypes.Name, user.DisplayName)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
