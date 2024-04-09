using Discord;
using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace ReactTracker.Discord.Events.Commands.Guild;

public class Config : BaseDiscordCommand
{
    public override string Name => "config";
    public override string Description => "Set a property for the guild";

    public override GuildPermission? MemberPermissions => GuildPermission.ManageGuild;

    public override SlashCommandBuilder GetBuilder ( )
    {
        return new SlashCommandBuilder( )
            .WithName( Name )
            .WithDescription( Description )
            .AddOption( new SlashCommandOptionBuilder( )
                .WithName( "property" )
                .WithDescription( "The property to set" )
                .WithRequired( true )
                .AddChoice( "Emoji", Models.GuildProperties.Emoji )
                .AddChoice( "Reaction Feed", Models.GuildProperties.ReactFeed )
                .WithType( ApplicationCommandOptionType.String )
            )
            .AddOption( new SlashCommandOptionBuilder( )
                .WithName( "value" )
                .WithDescription( "The value to set the property to" )
                .WithRequired( false )
                .WithType( ApplicationCommandOptionType.String )
            );
    }

    public override async Task Execute ( SocketSlashCommand command, SlashCommandHandler commandHandler )
    {
        if ( !command.GuildId.HasValue )
        {
            await command.RespondAsync( "This command must be run in a guild channel", ephemeral: true );
            return;
        }

        if (command.Channel is not SocketGuildChannel channel)
        {
            await command.RespondAsync( "This command must be run in a guild channel", ephemeral: true );
            return;
        }

        var guildId = command.GuildId.Value;
        var property = command.Data.Options.First( o => o.Name == "property" ).Value.ToString( );
        var valueArg = command.Data.Options.FirstOrDefault(o => o.Name == "value");
        var value = valueArg?.Value?.ToString( ) ?? "";

        if ( property == null || value == null )
        {
            await command.RespondAsync( "The command input was malformed", ephemeral: true );
            return;
        }

        // Add additional checks depending on the property
        if ( property == Models.GuildProperties.Emoji && valueArg != null)
        {
            if ( Emoji.TryParse( value, out var emoji ) )
            {
                value = emoji.Name;
            }
            else if ( Emote.TryParse( value, out var emote ) )
            {
                if (channel.Guild.Emotes.FirstOrDefault(g => g.Id == emote.Id) == null)
                {
                    await command.RespondAsync( $"`{value}` is not a valid emoji/emote. Make sure your emote is from this guild.", ephemeral: true );
                    return;
                }
                value = emote.Id.ToString( );
            }
            else
            {
                await command.RespondAsync( $"`{value}` is not a valid emoji/emote. Please try again.", ephemeral: true );
                return;
            }
        } else if (property == Models.GuildProperties.ReactFeed && valueArg != null)
        {
            if ( !value.StartsWith( "<#" ) || !value.EndsWith( '>' ) )
            {
                await command.RespondAsync( $"`{value}` is not a valid channel. Please try again. You can tag the channel directly by starting your value with # and clicking the channel you want.", ephemeral: true );
                return;
            }

            value = value[2..^1];
            if (!ulong.TryParse( value, out _ ))
            {
                await command.RespondAsync( $"`{value}` is not a valid channel. Please try again. You can tag the channel directly by starting your value with # and clicking the channel you want.", ephemeral: true );
                return;
            }

            var client = commandHandler.GetClient();
            var s = client.GetGuild( guildId );

            if (channel == null)
            {
                await command.RespondAsync( $"`{value}` is not a valid channel. Please try again. You can tag the channel directly by starting your value with # and clicking the channel you want.", ephemeral: true );
                return;
            }
        }

        await commandHandler.GetGuildCache( ).UpdateAsync( guildId, g =>
        {
            if (valueArg == null)
                g.Properties.Remove(property);
            else
                g.Properties[property] = value;
        } );

        await command.RespondAsync( $"Set property `{property}` to `{(valueArg == null ? "no value" : value)}` for this guild", ephemeral: true );
    }
}
