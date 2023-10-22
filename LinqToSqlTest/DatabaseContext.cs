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
        modelBuilder.Entity<Book>().ToTable(nameof(Book));
        modelBuilder.Entity<Book>().HasKey(e => e.Id);
    }
}