using Discord;
using Discord.WebSocket;
using ReactTracker.Data;
using ReactTracker.Database;
using ReactTracker.Models;

namespace ReactTracker.Discord.Events;

public class ReactionModified
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordGuildCache _guildCache;
    private readonly ILogger<DiscordBot> _logger;

    public ReactionModified (
        DiscordSocketClient client,
        ILogger<DiscordBot> logger,
        IServiceScopeFactory scopeFactory,
        DiscordGuildCache guildCache
        )
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _guildCache = guildCache;
        _logger = logger;

        _client.ReactionAdded += ReactionAdded;
        _client.ReactionRemoved += ReactionRemoved;
        _client.ReactionsRemovedForEmote += ReactionsRemovedForEmote;
        _client.ReactionsCleared += ReactionsCleared;
    }

    private async Task ReactionsCleared ( Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel )
    {
        // Make sure we're in a guild 
        var msg = await message.GetOrDownloadAsync( );
        if ( msg is null )
        {
            _logger.LogWarning( "Message not found" );
            return;
        }

        if ( msg.Channel is not IGuildChannel )
            return;

        // Clear the reaction list on the post
        using var scope = _scopeFactory.CreateScope( );
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>( );

        var post = await db.Posts.FindAsync( msg.Id );
        if ( post is null )
            return;

        post.ReactionIds.Clear( );
        await db.SaveChangesAsync( );
    }

    private async Task ReactionsRemovedForEmote ( Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, IEmote emote )
    {
        // Make sure we're in a guild 
        var msg = await message.GetOrDownloadAsync( );
        if ( msg is null )
        {
            _logger.LogWarning( "Message not found" );
            return;
        }

        if ( msg.Channel is not IGuildChannel guildChannel )
            return;

        // Make sure the reaction is the guild's configured emoji
        if ( !await IsGuildEmoji( guildChannel.GuildId, emote ) )
            return;

        // Clear the reaction list on the post
        using var scope = _scopeFactory.CreateScope( );
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>( );

        var post = await db.Posts.FindAsync( msg.Id );
        if ( post is null )
            return;

        post.ReactionIds.Clear( );
        await db.SaveChangesAsync( );
    }

    private async Task ReactionAdded ( Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction )
    {
        // Make sure we're in a guild 
        var msg = await message.GetOrDownloadAsync( );
        if ( msg is null )
        {
            _logger.LogWarning( "Message not found" );
            return;
        }

        if (msg.Author.IsBot || msg.Author.IsWebhook) // Ignore bot reactions
            return;

        if (reaction.User.IsSpecified && reaction.User.Value.IsBot) // Ignore bot reactions
            return;

        if (msg.Author.Id == reaction.UserId) // Ignore self reactions
            return;

        if ( msg.Channel is not IGuildChannel guildChannel )
            return;

        // Make sure the reaction is the guild's configured emoji
        if ( !await IsGuildEmoji( guildChannel.GuildId, reaction.Emote ) )
            return;

        // Add the reaction to the Post
        using var scope = _scopeFactory.CreateScope( );
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>( );

        var post = await db.Posts.FindAsync( msg.Id );
        if ( post is null )
        {
            post = new Post
            {
                Id = msg.Id,
                GuildId = guildChannel.GuildId,
                AuthorId = msg.Author.Id,
                ChannelId = msg.Channel.Id,
                Timestamp = msg.Timestamp.ToUniversalTime()
            };

            db.Posts.Add( post );
        }

        // Make sure the user didn't already react to this post
        if ( post.ReactionIds.Contains( reaction.UserId ) )
            return;

        post.ReactionIds.Add( reaction.UserId );
        await db.SaveChangesAsync( );
    }

    private async Task ReactionRemoved ( Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction )
    {
        // Make sure we're in a guild 
        var msg = await message.GetOrDownloadAsync( );
        if ( msg is null )
        {
            _logger.LogWarning( "Message not found" );
            return;
        }

        if (msg.Author.IsBot || msg.Author.IsWebhook) // Ignore bot reactions
            return;

        if (reaction.User.IsSpecified && reaction.User.Value.IsBot) // Ignore bot reactions
            return;

        if (msg.Author.Id == reaction.UserId) // Ignore self reactions
            return;

        if ( msg.Channel is not IGuildChannel guildChannel )
            return;

        // Make sure the reaction is the guild's configured emoji
        if ( !await IsGuildEmoji( guildChannel.GuildId, reaction.Emote ) )
            return;

        // Remove the reaction from the Post
        using var scope = _scopeFactory.CreateScope( );
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>( );

        var post = await db.Posts.FindAsync( msg.Id );
        if ( post is null )
            return;

        // Make sure the user didn't already react to this post
        if ( !post.ReactionIds.Contains( reaction.UserId ) )
            return;

        post.ReactionIds.Remove( reaction.UserId );
        await db.SaveChangesAsync( );
    }

    private async Task<bool> IsGuildEmoji ( ulong guildId, IEmote reaction )
    {
        string? emoteId;

        // Check if emoteName is an emoji
        if ( reaction is Emoji emoji )
        {
            emoteId = emoji.Name;
        }
        else if ( reaction is Emote emote )
        {
            emoteId = emote.Id.ToString( );
        }
        else
            return false;

        // Get the guild
        var guild = await _guildCache.GetAsync( guildId );
        if ( !guild.Properties.TryGetValue( Models.GuildProperties.Emoji, out var guildEmoji ) )
            return false; // No emoji set for this discord

        return emoteId == guildEmoji;
    }
}
