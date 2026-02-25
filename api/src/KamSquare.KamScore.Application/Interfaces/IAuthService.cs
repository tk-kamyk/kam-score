using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Application.Interfaces;

public interface IAuthService
{
    AuthResult? Authenticate(string username, string password);
}
