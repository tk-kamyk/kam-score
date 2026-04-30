namespace KamSquare.KamScore.Application.Interfaces;

public interface IDatabaseHealthProbe
{
    Task<bool> IsReachableAsync(CancellationToken cancellationToken = default);
}
