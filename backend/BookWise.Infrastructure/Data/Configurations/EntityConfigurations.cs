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

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(u => u.GoogleSubject).HasColumnName("google_subject").HasMaxLength(128);
        builder.Property(u => u.PhoneNumberE164).HasColumnName("phone_number_e164").HasMaxLength(20);
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasIndex(u => u.GoogleSubject).IsUnique().HasFilter("google_subject IS NOT NULL");
        builder.HasIndex(u => u.PhoneNumberE164).IsUnique().HasFilter("phone_number_e164 IS NOT NULL");
        builder.HasIndex(u => u.Email).HasFilter("email IS NOT NULL");
        builder.HasQueryFilter(u => u.IsActive);
    }
}

public class LoginOtpConfiguration : IEntityTypeConfiguration<LoginOtp>
{
    public void Configure(EntityTypeBuilder<LoginOtp> builder)
    {
        builder.ToTable("login_otps");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(o => o.PhoneNumberE164).HasColumnName("phone_number_e164").IsRequired().HasMaxLength(20);
        builder.Property(o => o.CodeHash).HasColumnName("code_hash").IsRequired().HasMaxLength(128);
        builder.Property(o => o.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(o => o.ConsumedAt).HasColumnName("consumed_at");
        builder.Property(o => o.Attempts).HasColumnName("attempts").HasDefaultValue(0);
        builder.Property(o => o.PilotMessageId).HasColumnName("pilot_message_id").HasMaxLength(64);
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.Property(o => o.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasIndex(o => o.PhoneNumberE164);
        builder.HasIndex(o => o.ExpiresAt);
        builder.HasQueryFilter(o => o.IsActive);
    }
}
