namespace KamSquare.KamScore.Domain.Entities;

public abstract class Entity
{
    public string Id { get; set; } = null!;
    public DateTime? LastModified { get; set; }
    public string? ETag { get; set; }
}
