
using Discord;
using Discord.WebSocket;
using ReactTracker.Data;
using ReactTracker.Database;
using ReactTracker.Discord.Events;
using ReactTracker.Discord.Events.Commands;

namespace ReactTracker.Discord;

public class DiscordBot (
    DiscordSocketClient client,
    ILogger<DiscordBot> logger,
    IServiceScopeFactory scopeFactory,
    DiscordGuildCache guildCache,
    SlashCommandHandler commandHandler ) : IHostedService
{
    // Consts for the bot
    public static readonly ulong OwnerId = 231930652663087104;

    private readonly DiscordSocketClient _client = client;
    private readonly ILogger<DiscordBot> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    private readonly DiscordGuildCache _guildCache = guildCache;
    private readonly SlashCommandHandler _commandHandler = commandHandler;

    public async Task StartAsync ( CancellationToken cancellationToken )
    {
        _logger.LogInformation( "Starting Discord bot" );
        // Set up default event handlers
        _client.Log += Log;

        // Register events 
        _ = new GuildUpdates( _client, _guildCache );
        _ = new ReactionModified( _client, _logger, _scopeFactory, _guildCache );
        _ = new PostDeleted(_client, _logger, _scopeFactory);
        _ = new DeveloperCommands( _client, _guildCache, _commandHandler);

        var token = Environment.GetEnvironmentVariable( "DISCORD_TOKEN" );
        if ( string.IsNullOrEmpty( token ) )
        {
            throw new InvalidOperationException( "DISCORD_TOKEN is not set" );
        }

        // Authenticate with Discord using the token 
        await _client.LoginAsync( TokenType.Bot, token );
        await _client.StartAsync( );
    }

    public (AppDbContext dbContext, IServiceScope scope) GetDatabase ( )
    {
        var scope = _scopeFactory.CreateScope( );
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>( );
        return (dbContext, scope);
    }

    private Task Log ( LogMessage message )
    {
        switch ( message.Severity )
        {
            case LogSeverity.Critical:
                _logger.LogCritical( "{message}", message.Message );
                break;
            case LogSeverity.Error:
                _logger.LogError( "{message}", message.Message );
                break;
            case LogSeverity.Warning:
                _logger.LogWarning( "{message}", message.Message );
                break;
            case LogSeverity.Info:
                _logger.LogInformation( "{message}", message.Message );
                break;
            case LogSeverity.Verbose:
                _logger.LogTrace( "{message}", message.Message );
                break;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync ( CancellationToken cancellationToken )
    {
        return Task.CompletedTask;
    }
}
