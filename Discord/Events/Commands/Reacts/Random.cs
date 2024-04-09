using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using ReactTracker.Data;
using ReactTracker.Models;

namespace ReactTracker.Discord.Events.Commands.Reacts;

public class Random : BaseDiscordCommand
{
    public override string Name => "random";

    public override string Description => "Fetches a random reacted quote (optionally by member)";

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
            Post randomPost;
            if ( command.Data.Options.FirstOrDefault( o => o.Name == "member" )?.Value is not SocketGuildUser member )
            {
                var rPost = dbContext.Posts.Where( p => p.ReactionIds.Count > 0 && p.GuildId == channel.Guild.Id ).OrderBy( r => EF.Functions.Random( ) ).FirstOrDefault( );
                if ( rPost is not null )
                {
                    randomPost = rPost;
                }
                else
                {
                    await command.RespondAsync( "No reacted quotes found", ephemeral: true );
                    return;
                }
            }
            else
            {
                var rPost = dbContext.Posts.Where( p => p.AuthorId == member.Id && p.ReactionIds.Count > 0 && p.GuildId == channel.Guild.Id ).OrderBy( r => EF.Functions.Random( ) ).FirstOrDefault( );
                if ( rPost is not null )
                {
                    randomPost = rPost;
                }
                else
                {
                    await command.RespondAsync( "There are no reacted quotes for that member!", ephemeral: true );
                    return;
                }
            }

            var postAuthor = await commandHandler.GetClient( ).GetUserAsync( randomPost.AuthorId );
            var clientChannel = await commandHandler.GetClient( ).GetChannelAsync( randomPost.ChannelId );
            if ( clientChannel is not ISocketMessageChannel postChannel )
            {
                await command.RespondAsync( "The channel for this quote could not be found", ephemeral: true );
                return;
            }

            var message = await postChannel.GetMessageAsync( randomPost.Id );
            if ( message is not IUserMessage userMessage )
            {
                await command.RespondAsync( "The message for this quote could not be found", ephemeral: true );
                return;
            }

            var embed = new EmbedBuilder( )
                .WithColor( Color.Orange )
                .WithAuthor( message.Author )
                .WithFooter( $"Sent {MessageHelpers.RelativeTime( randomPost.Timestamp )}" );

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
            var guild = await commandHandler.GetGuildCache( ).GetAsync( randomPost.GuildId );

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
                await command.RespondAsync( embeds: [embed.Build( ), otherEmbed], text: $"{emojiParsed} **{randomPost.ReactionIds.Count}** | {message.GetJumpUrl( )} ({message.Author.Mention})", allowedMentions: AllowedMentions.None );
                return;
            }

            await command.RespondAsync( embed: embed.Build( ), text: $"{emojiParsed} **{randomPost.ReactionIds.Count}** | {message.GetJumpUrl( )} ({message.Author.Mention})", allowedMentions: AllowedMentions.None );
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
                .WithRequired( false )
                .WithType( ApplicationCommandOptionType.User ) );
    }
}
