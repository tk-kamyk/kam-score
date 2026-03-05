namespace KamSquare.KamScore.Domain.Exceptions;

public class PhaseStateException : DomainException
{
    public PhaseStateException(string phaseName, string operation, string reason)
        : base($"Cannot {operation} phase '{phaseName}': {reason}.") { }
}
