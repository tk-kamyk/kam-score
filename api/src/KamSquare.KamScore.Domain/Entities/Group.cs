namespace KamSquare.KamScore.Domain.Entities;

public class Group
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> TeamIds { get; set; } = [];

    public static Group Create(string name)
    {
        return new Group
        {
            Id = Guid.NewGuid().ToString(),
            Name = name
        };
    }

    public void Update(string name)
    {
        Name = name;
    }
}
