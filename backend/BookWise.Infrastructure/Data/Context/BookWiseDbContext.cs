using BookWise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookWise.Infrastructure.Data.Context;

public class BookWiseDbContext : DbContext
{
    public BookWiseDbContext(DbContextOptions<BookWiseDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookWiseDbContext).Assembly);
    }
}
