using Microsoft.EntityFrameworkCore;
using ReactTracker.Database;
using ReactTracker.Discord;

var builder = WebApplication.CreateBuilder( args );

// Set up the database 
var connectionString = builder.Configuration.GetConnectionString( "DefaultConnection" ) ?? throw new InvalidOperationException( "Connection string 'DefaultConnection' not found." );
builder.Services.AddDbContext<AppDbContext>( options => options.UseSqlite( connectionString ) );

// Add MemoryCache
builder.Services.AddMemoryCache( );

// Set up the Discord bot
builder.ConfigureDiscordBot( );

var app = builder.Build( );

// If we're in production, run a database migration
if ( app.Environment.IsProduction( ) )
{
    using var scope = app.Services.CreateScope( );
    await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>( );
    await dbContext.Database.MigrateAsync( );
}

app.Run( );
