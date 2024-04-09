using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ReactTracker.Models;
using System.Text.Json;

namespace ReactTracker.Database;

public partial class AppDbContext : DbContext
{
    public DbSet<Guild> Guilds => Set<Guild>( );

    protected override void OnModelCreating ( ModelBuilder modelBuilder )
    {
        base.OnModelCreating( modelBuilder );

        modelBuilder.Entity<Guild>( )
            .Property( b => b.Properties )
            .HasConversion(
                v => JsonSerializer.Serialize( v, null as JsonSerializerOptions ),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>( v, null as JsonSerializerOptions ),
                    new ValueComparer<Dictionary<string, string>>(
                        ( c1, c2 ) => c1.SequenceEqual( c2 ),
                        c => c.Aggregate( 0, ( a, v ) => HashCode.Combine( a, v.GetHashCode( ) ) ),
                        c => c.ToDictionary( e => e.Key, e => e.Value ) ) );
    }
}
