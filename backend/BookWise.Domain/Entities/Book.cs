namespace BookWise.Domain.Entities;

public class Book : BaseEntity
{
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? CoverImageUrl { get; private set; }
    public int PublicationYear { get; private set; }
    public string? ISBN { get; private set; }
    public int AuthorId { get; private set; }
    public int GenreId { get; private set; }

    public virtual Author Author { get; private set; } = null!;
    public virtual Genre Genre { get; private set; } = null!;

    protected Book() { }

    public Book(string title, string? description, int publicationYear, string? isbn, int authorId, int genreId, string? coverImageUrl = null)
    {
        Title = title;
        Description = description;
        PublicationYear = publicationYear;
        ISBN = isbn;
        AuthorId = authorId;
        GenreId = genreId;
        CoverImageUrl = coverImageUrl;
    }

    public void Update(string title, string? description, int publicationYear, string? isbn, int authorId, int genreId, string? coverImageUrl = null)
    {
        Title = title;
        Description = description;
        PublicationYear = publicationYear;
        ISBN = isbn;
        AuthorId = authorId;
        GenreId = genreId;
        CoverImageUrl = coverImageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDescription(string description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
