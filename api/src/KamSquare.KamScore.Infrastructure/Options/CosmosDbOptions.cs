namespace KamSquare.KamScore.Infrastructure.Options;

public class CosmosDbOptions
{
    public const string SectionName = "CosmosDb";
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "KamScore";
}
