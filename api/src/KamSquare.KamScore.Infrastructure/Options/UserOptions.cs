namespace KamSquare.KamScore.Infrastructure.Options;

public class UserOptions
{
    public const string SectionName = "Users";
    public List<UserEntry> Entries { get; set; } = [];
}

public class UserEntry
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}
