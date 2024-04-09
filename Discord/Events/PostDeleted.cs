using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using ReactTracker.Database;

namespace ReactTracker.Discord.Events;

public class PostDeleted
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DiscordBot> _logger;

    public PostDeleted (
        DiscordSocketClient client,
        ILogger<DiscordBot> logger,
        IServiceScopeFactory scopeFactory
        )
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _client.MessageDeleted += MessageDeleted;
        _client.MessagesBulkDeleted += MessageBulkDeleted;
    }

    private async Task MessageDeleted ( Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel )
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var post = await db.Posts.FindAsync(message.Id);
        if (post is null)
            return;

        db.Posts.Remove(post);
        await db.SaveChangesAsync();
    }

    private async Task MessageBulkDeleted ( IReadOnlyCollection<Cacheable<IMessage, ulong>> collection, Cacheable<IMessageChannel, ulong> cacheable )
    {
        using var scope = _scopeFactory.CreateScope( );
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>( );

        var messageIds = collection.Select( c => c.Id ).ToList( );

        var affected = await db.Posts.Where( p => messageIds.Contains( p.Id ) ).ToListAsync( );
        if (affected.Count == 0)
            return;

        db.Posts.RemoveRange( affected );
        await db.SaveChangesAsync( );
    }
}
