namespace KamSquare.KamScore.Domain.Entities;

/// <summary>
/// Nested value object within Phase — not an independent entity.
/// </summary>
public class Level
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }

    public static Level Create(string name, int order)
    {
        return new Level
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Order = order
        };
    }

    public void Update(string name) => Name = name;
}
