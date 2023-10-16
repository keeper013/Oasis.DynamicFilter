namespace LinqToSqlTest;

using Microsoft.EntityFrameworkCore;

internal class DatabaseContext : DbContext
{
    private static readonly EntityState[] _states = new[] { EntityState.Added, EntityState.Modified };

    public DatabaseContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entity1>().ToTable(nameof(Entity1));
        modelBuilder.Entity<Entity1>().HasKey(e => e.Id);
    }
}