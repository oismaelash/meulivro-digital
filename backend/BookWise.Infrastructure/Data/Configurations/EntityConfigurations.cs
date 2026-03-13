using BookWise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookWise.Infrastructure.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(b => b.Title).HasColumnName("title").IsRequired().HasMaxLength(300);
        builder.Property(b => b.Description).HasColumnName("description");
        builder.Property(b => b.PublicationYear).HasColumnName("publication_year").IsRequired();
        builder.Property(b => b.ISBN).HasColumnName("isbn").HasMaxLength(13);
        builder.Property(b => b.CoverImageUrl).HasColumnName("cover_image_url");
        builder.Property(b => b.AuthorId).HasColumnName("author_id");
        builder.Property(b => b.GenreId).HasColumnName("genre_id");
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasOne(b => b.Author)
            .WithMany(a => a.Books)
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Genre)
            .WithMany(g => g.Books)
            .HasForeignKey(b => b.GenreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.ISBN).IsUnique().HasFilter("isbn IS NOT NULL");
        builder.HasIndex(b => b.Title);
        builder.HasQueryFilter(b => b.IsActive);
    }
}

public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("authors");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(a => a.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(a => a.Biography).HasColumnName("biography");
        builder.Property(a => a.Nationality).HasColumnName("nationality").HasMaxLength(100);
        builder.Property(a => a.BirthDate).HasColumnName("birth_date");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasIndex(a => a.Name);
        builder.HasQueryFilter(a => a.IsActive);
    }
}

public class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.ToTable("genres");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(g => g.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(g => g.Description).HasColumnName("description");
        builder.Property(g => g.CreatedAt).HasColumnName("created_at");
        builder.Property(g => g.UpdatedAt).HasColumnName("updated_at");
        builder.Property(g => g.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasIndex(g => g.Name).IsUnique();
        builder.HasQueryFilter(g => g.IsActive);
    }
}
