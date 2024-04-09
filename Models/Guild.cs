namespace ReactTracker.Models;

public class Guild
{
    public ulong Id { get; set; }
    public string Name { get; set; }
    public Dictionary<string, string> Properties { get; set; }

    public Guild ( )
    {
        Name = string.Empty;
        Properties = [];
    }
}

public static class GuildProperties
{
    public const string Emoji = "emoji";
    public const string ReactFeed = "reactFeed";
}