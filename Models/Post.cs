namespace ReactTracker.Models;

public class Post
{
    public ulong Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong AuthorId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public List<ulong> ReactionIds { get; set; }

    public Post ( )
    {
        ReactionIds = [];
    }
}
