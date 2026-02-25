namespace KamSquare.KamScore.Domain.Exceptions;

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message) { }

    public ForbiddenException()
        : base("You do not have permission to perform this action.") { }
}
