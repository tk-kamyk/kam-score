namespace KamSquare.KamScore.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public const int DefaultExpirationMinutes = 480; // 8 hours
    public int ExpirationMinutes { get; set; } = DefaultExpirationMinutes;
}
