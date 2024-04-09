using Discord;
using Discord.WebSocket;

namespace ReactTracker.Discord.Events.Commands.Reacts;

public class Leaderboard : BaseDiscordCommand
{
    public override string Name => "leaderboard";

    public override string Description => "Shows the react leaderboard - Top 10 and your placement";

    public override GuildPermission? MemberPermissions => null;

    public override async Task Execute ( SocketSlashCommand command, SlashCommandHandler commandHandler )
    {
        // Make sure we're actually in a guild 
        if ( command.Channel is not SocketGuildChannel channel )
        {
            await command.RespondAsync( "This command must be run in a guild channel", ephemeral: true );
            return;
        }

        // Get the top 10 reacted users
        var (dbContext, scope) = commandHandler.GetDatabase( );
        try
        {
            var leaderboard = dbContext.Posts
                .Where( p => p.GuildId == channel.Guild.Id )
                .GroupBy( p => p.AuthorId )
                .Select( g => new LeaderboardEntry
                {
                    UserId = g.Key,
                    TotalReactions = g.Sum( p => p.ReactionIds.Count ),
                    TotalMessagesWithReactions = g.Count( p => p.ReactionIds.Count > 0 )
                } )
                .OrderByDescending( e => e.TotalReactions )
                .ToList( );

            // Find the player's position on the leaderboard
            var userEntry = leaderboard.FirstOrDefault( e => e.UserId == command.User.Id );
            var userPosition = userEntry != null ? leaderboard.IndexOf( userEntry ) + 1 : leaderboard.Count + 1;
            var top10 = leaderboard.Take( 10 ).ToList( );

            // Get the react emoji from this guild 
            var guild = await commandHandler.GetGuildCache( ).GetAsync( channel.Guild.Id );

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

            // Get the list of react counts seperated by \n
            var top10Users = string.Join( "\n", top10.Select( e => $"<@{e.UserId}>" ) );
            var top10Reacts = string.Join( "\n", top10.Select( e => e.TotalReactions ) );
            var top10Messages = string.Join( "\n", top10.Select( e => e.TotalMessagesWithReactions ) );

            var embed = new EmbedBuilder( )
                .WithColor( Color.Orange )
                .WithTitle( $"{emojiParsed} Reaction Leaderboard" )
                .WithDescription( $"Your position: {userPosition}" )
                .AddField( "User", top10Users, true )
                .AddField( $"{emojiParsed} Reacts", top10Reacts, true )
                .AddField( $"Messages", top10Messages, true );

            await command.RespondAsync( embed: embed.Build( ) );
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
            .WithDescription( Description );
    }
}

public class LeaderboardEntry
{
    public ulong UserId { get; set; }
    public int TotalReactions { get; set; }
    public int TotalMessagesWithReactions { get; set; }

}