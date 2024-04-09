using Discord;
using Microsoft.Extensions.Caching.Memory;
using ReactTracker.Database;
using ReactTracker.Models;

namespace ReactTracker.Data;

public class DiscordGuildCache ( IMemoryCache memoryCache, IServiceProvider serviceProvider )
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private async Task<Guild> GetAsync ( AppDbContext db, ulong guildId )
    {
        var dbGuild = await db.Guilds.FindAsync( guildId );
        if ( dbGuild is not null )
        {
            _memoryCache.Set( guildId, dbGuild );
            return dbGuild;
        }

        var guild = new Guild { Id = guildId, Name = "New Guild" };
        db.Guilds.Add( guild );

        await db.SaveChangesAsync( );
        _memoryCache.Set( guildId, guild );

        return guild;
    }

    public async Task<Guild> GetAsync ( ulong guildId )
    {
        if ( _memoryCache.TryGetValue<Guild>( guildId, out var cache ) )
        {
            return cache!;
        }

        using var scope = _serviceProvider.CreateScope( );
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>( );

        return await GetAsync( db, guildId );
    }

    public Task<Guild> GetAsync ( IGuild guild )
    {
        return GetAsync( guild.Id );
    }

    public async Task UpdateAsync ( ulong guildId, Action<Guild> updateAction )
    {
        var scope = _serviceProvider.CreateScope( );
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>( );

        var guild = await GetAsync( db, guildId );
        updateAction( guild );

        db.Guilds.Update( guild );
        await db.SaveChangesAsync( );

        _memoryCache.Set( guildId, guild );
    }
}