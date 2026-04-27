using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Domain.Services.Formats;

public static class CustomGenerator
{
    /// <summary>
    /// Custom-format phases do not generate games — standings are entered manually.
    /// Always returns an empty list regardless of team count.
    /// </summary>
    public static List<Game> Generate() => [];
}
