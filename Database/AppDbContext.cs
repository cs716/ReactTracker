using Microsoft.EntityFrameworkCore;

namespace ReactTracker.Database;

public partial class AppDbContext (DbContextOptions<AppDbContext> options) : DbContext(options)
{
}