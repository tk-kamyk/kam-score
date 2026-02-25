namespace KamSquare.KamScore.Domain.Exceptions;

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message) : base(message) { }

    public UnauthorizedException()
        : base("Authentication is required.") { }
}
