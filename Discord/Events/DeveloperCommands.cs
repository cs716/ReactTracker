using Discord;
using Discord.WebSocket;
using ReactTracker.Data;
using ReactTracker.Discord.Events.Commands;

namespace ReactTracker.Discord.Events;

public class DeveloperCommands
{
    private readonly DiscordSocketClient _client;
    private readonly DiscordGuildCache _guildCache;
    private readonly SlashCommandHandler _commandHandler;
    private const string Prefix = "$";

    public DeveloperCommands ( DiscordSocketClient client, DiscordGuildCache guildCache, SlashCommandHandler commandHandler )
    {
        _client = client;
        _guildCache = guildCache;
        _commandHandler = commandHandler;

        _client.MessageReceived += MessageReceived;
    }

    private async Task MessageReceived ( SocketMessage msg )
    {
        var content = msg.Content;

        if ( !content.StartsWith( Prefix ) )
            return;

        if ( msg.Author.Id != DiscordBot.OwnerId )
            return;

        // Extract the command from the message
        var command = content[1..].Split( ' ' )[0];
        var args = content[1..].Split( ' ' )[1..];

        if ( command == "ping" )
        {
            await msg.Channel.SendMessageAsync( "Pong!" );
        }
        else if ( command == "guildset" )
        {
            if ( msg.Channel is not SocketGuildChannel channel )
            {
                await msg.Channel.SendMessageAsync( "This command must be run in a guild channel", messageReference: new MessageReference( msg.Id ) );
                return;
            }

            // Make sure there are two args
            if ( args.Length != 2 )
            {
                await msg.Channel.SendMessageAsync( "Usage: $guildset <property> <value>", messageReference: new MessageReference( msg.Id ) );
                return;
            }

            await _guildCache.UpdateAsync( channel.Guild.Id, g =>
            {
                g.Properties[args[0]] = args[1];
            } );

            await msg.Channel.SendMessageAsync( $"Guild property `{args[0]}` set to `{args[1]}`", messageReference: new MessageReference( msg.Id ) );
        }
        else if ( command == "registerglobal" )
        {
            if ( args.Length != 1 )
            {
                await msg.Channel.SendMessageAsync( "Usage: $registerglobal <commandName>", messageReference: new MessageReference( msg.Id ) );
                return;
            }

            var commandName = args[0];
            var cmd = _commandHandler.GetCommand( commandName );
            if ( cmd is null )
            {
                await msg.Channel.SendMessageAsync( $"Command `{commandName}` not found", messageReference: new MessageReference( msg.Id ) );
                return;
            }

            var commandProperties = cmd.GetBuilder( );
            if ( cmd.MemberPermissions.HasValue )
            {
                commandProperties.WithDefaultMemberPermissions( cmd.MemberPermissions );
            }

            await _client.CreateGlobalApplicationCommandAsync( commandProperties.Build( ) );

            await msg.Channel.SendMessageAsync( $"Command `{commandName}` registered globally", messageReference: new MessageReference( msg.Id ) );
        }
    }
}
