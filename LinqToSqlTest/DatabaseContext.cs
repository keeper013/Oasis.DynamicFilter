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
        modelBuilder.Entity<Book>().HasKey(b => b.Id);
        modelBuilder.Entity<Author>().ToTable(nameof(Author));
        modelBuilder.Entity<Author>().HasKey(a => a.Id);
        modelBuilder.Entity<Author>().HasMany(a => a.Books).WithOne().HasForeignKey(b => b.AuthorId).OnDelete(DeleteBehavior.NoAction);
    }
}