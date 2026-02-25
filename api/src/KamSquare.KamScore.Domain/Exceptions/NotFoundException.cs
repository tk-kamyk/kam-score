namespace KamSquare.KamScore.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string entityName, string id)
        : base($"{entityName} with id '{id}' was not found.") { }
}
