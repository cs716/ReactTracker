using Discord.WebSocket;
using ReactTracker.Data;
using ReactTracker.Database;
using ReactTracker.Discord.Events.Commands.Guild;
using ReactTracker.Discord.Events.Commands.Reacts;
using Random = ReactTracker.Discord.Events.Commands.Reacts.Random;

namespace ReactTracker.Discord.Events.Commands;

public class SlashCommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<SlashCommandHandler> _logger;
    private readonly DiscordGuildCache _discordGuildCache;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly List<BaseDiscordCommand> _commands;

    public SlashCommandHandler ( DiscordSocketClient client, ILogger<SlashCommandHandler> logger, DiscordGuildCache discordGuildCache, IServiceScopeFactory scopeFactory )
    {
        _client = client;
        _logger = logger;
        _discordGuildCache = discordGuildCache;
        _scopeFactory = scopeFactory;
        _commands = [];

        _client.SlashCommandExecuted += SlashCommandExecuted;

        _logger.LogInformation( "Slash command handler registered" );
        RegisterDefaults( );
    }

    private void RegisterDefaults ( )
    {
        RegisterCommand( new Config( ) );
        RegisterCommand( new Random( ) );
        RegisterCommand( new Leaderboard( ) );
        RegisterCommand( new TopComment( ) );
    }

    public BaseDiscordCommand? GetCommand ( string name )
    {
        return _commands.FirstOrDefault( c => c.Name == name );
    }

    public DiscordGuildCache GetGuildCache ( )
    {
        return _discordGuildCache;
    }

    public ILogger<SlashCommandHandler> GetLogger ( )
    {
        return _logger;
    }

    public DiscordSocketClient GetClient ( )
    {
        return _client;
    }

    public (AppDbContext dbContext, IServiceScope scope) GetDatabase ( )
    {
        var scope = _scopeFactory.CreateScope( );
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>( );
        return (dbContext, scope);
    }

    public void RegisterCommand ( BaseDiscordCommand command )
    {
        _commands.Add( command );
    }

    private Task SlashCommandExecuted ( SocketSlashCommand command )
    {
        try
        {
            var cmd = GetCommand( command.Data.Name );
            if ( cmd != null )
            {
                return cmd.Execute( command, this );

            }
        }
        catch ( Exception e )
        {
            _logger.LogError( e, "Error executing command" );
        }

        return Task.CompletedTask;
    }
}
