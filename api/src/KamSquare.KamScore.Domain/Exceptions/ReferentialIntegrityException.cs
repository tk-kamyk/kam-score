namespace KamSquare.KamScore.Domain.Exceptions;

public class ReferentialIntegrityException : DomainException
{
    public ReferentialIntegrityException(string entityType, string entityName, string reason)
        : base($"Cannot delete {entityType} '{entityName}': {reason}.") { }
}
