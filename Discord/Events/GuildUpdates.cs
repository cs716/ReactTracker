using Discord.WebSocket;
using ReactTracker.Data;

namespace ReactTracker.Discord.Events;

public class GuildUpdates
{
    private readonly DiscordGuildCache _guildCache;

    public GuildUpdates ( DiscordSocketClient client, DiscordGuildCache guildCache )
    {
        _guildCache = guildCache;

        client.JoinedGuild += RegisterGuild;
        client.GuildUpdated += UpdateGuild;
    }

    private Task UpdateGuild ( SocketGuild guild1, SocketGuild guild2 )
    {
        return RegisterGuild( guild2 );
    }

    private Task RegisterGuild ( SocketGuild guild )
    {
        return _guildCache.UpdateAsync( guild.Id, g => g.Name = guild.Name );
    }
}