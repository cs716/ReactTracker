using Discord;
using Discord.WebSocket;
using ReactTracker.Data;
using ReactTracker.Discord.Events.Commands;

namespace ReactTracker.Discord;

public static class DiscordBotExtension
{
    public static WebApplicationBuilder ConfigureDiscordBot ( this WebApplicationBuilder builder )
    {
        DiscordSocketConfig config = new( )
        {
            GatewayIntents = GatewayIntents.All,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 500,
            UseInteractionSnowflakeDate = false
        };

        DiscordSocketClient client = new( config );

        builder.Services
            .AddSingleton( client )
            .AddSingleton<DiscordGuildCache>( )
            .AddSingleton<SlashCommandHandler>( )
            .AddHostedService<DiscordBot>( );

        return builder;
    }
}
