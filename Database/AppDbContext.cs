using Microsoft.EntityFrameworkCore;
using ReactTracker.Models;

namespace ReactTracker.Database;

public partial class AppDbContext ( DbContextOptions<AppDbContext> options ) : DbContext( options )
{
    public DbSet<Post> Posts => Set<Post>( );
}