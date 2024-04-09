using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using ReactTracker.Data;

namespace ReactTracker.Discord.Events.Commands.Reacts;

public class TopComment : BaseDiscordCommand
{
    public override string Name => "top";

    public override string Description => "Fetches a user's top reacted comment";

    public override GuildPermission? MemberPermissions => null;

    public override async Task Execute ( SocketSlashCommand command, SlashCommandHandler commandHandler )
    {
        if ( command.Channel is not SocketGuildChannel channel )
        {
            await command.RespondAsync( "This command must be run in a guild channel", ephemeral: true );
            return;
        }

        var (dbContext, scope) = commandHandler.GetDatabase( );
        try
        {
            if ( command.Data.Options.FirstOrDefault( o => o.Name == "member" )?.Value is not SocketGuildUser member )
            {
                await command.RespondAsync( "You must specify a member", ephemeral: true );
                return;
            }

            var post = await dbContext.Posts
                .Where( p => p.AuthorId == member.Id && p.GuildId == channel.Guild.Id )
                .OrderByDescending( p => p.ReactionIds.Count )
                .FirstOrDefaultAsync( );

            if ( post is null )
            {
                await command.RespondAsync( "There are no reacted messages found for that member", ephemeral: true );
                return;
            }

            var clientChannel = await commandHandler.GetClient( ).GetChannelAsync( post.ChannelId );
            if ( clientChannel is not ISocketMessageChannel postChannel )
            {
                await command.RespondAsync( "The channel for this message could not be found", ephemeral: true );
                return;
            }

            var message = await postChannel.GetMessageAsync( post.Id );
            if ( message is not IUserMessage userMessage )
            {
                await command.RespondAsync("The message for this message could not be found", ephemeral: true );
                return;
            }

            var embed = new EmbedBuilder( )
                .WithColor( Color.Orange )
                .WithAuthor( message.Author )
                .WithFooter( $"Sent {MessageHelpers.RelativeTime( post.Timestamp )}" );

            // Make sure the message content isn't above the maximum embed description
            if ( message.Content.Length > 2048 )
            {
                embed.WithDescription( message.Content[..2045] + "..." );
            }
            else
            {
                embed.WithDescription( message.Content );
            }

            // Get the react emoji from this guild 
            var guild = await commandHandler.GetGuildCache( ).GetAsync( post.GuildId );

            var emojiParsed = string.Empty;
            if ( guild is not null )
            {
                var hasProperty = guild.Properties.TryGetValue( Models.GuildProperties.Emoji, out var guildEmoji );
                if ( hasProperty )
                {
                    if ( Emoji.TryParse( guildEmoji, out var emoji ) )
                    {
                        emojiParsed = emoji.Name;
                    }
                    else if ( ulong.TryParse( guildEmoji, out var emote ) )
                    {
                        var guildEmote = await channel.Guild.GetEmoteAsync( emote );
                        if ( guildEmote is not null )
                        {
                            emojiParsed = guildEmote.ToString( );
                        }
                    }
                }
            }

            if ( message.Embeds.Count > 0 )
            {
                var otherEmbed = ( Embed ) message.Embeds.First( );
                await command.RespondAsync( embeds: [embed.Build( ), otherEmbed], text: $"{emojiParsed} **{post.ReactionIds.Count}** | {message.GetJumpUrl( )} ({message.Author.Mention})", allowedMentions: AllowedMentions.None );
                return;
            }

            await command.RespondAsync( embed: embed.Build( ), text: $"{emojiParsed} **{post.ReactionIds.Count}** | {message.GetJumpUrl( )} ({message.Author.Mention})", allowedMentions: AllowedMentions.None );
        }
        finally
        {
            scope.Dispose( );
        }
    }


    public override SlashCommandBuilder GetBuilder ( )
    {
        return new SlashCommandBuilder( )
            .WithName( Name )
            .WithDescription( Description )
            .AddOption( new SlashCommandOptionBuilder( )
                .WithName( "member" )
                .WithDescription( "The member to fetch a quote for" )
                .WithRequired( true )
                .WithType( ApplicationCommandOptionType.User ) );
    }
}
