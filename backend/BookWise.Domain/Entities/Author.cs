namespace BookWise.Domain.Entities;

public class Author : BaseEntity
{
    public int UserAccountId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Biography { get; private set; }
    public string? Nationality { get; private set; }
    public DateTime? BirthDate { get; private set; }

    public virtual ICollection<Book> Books { get; private set; } = new List<Book>();

    protected Author() { }

    public Author(int userAccountId, string name, string? biography, string? nationality, DateTime? birthDate)
    {
        UserAccountId = userAccountId;
        Name = name;
        Biography = biography;
        Nationality = nationality;
        BirthDate = birthDate;
    }

    public void Update(string name, string? biography, string? nationality, DateTime? birthDate)
    {
        Name = name;
        Biography = biography;
        Nationality = nationality;
        BirthDate = birthDate;
        UpdatedAt = DateTime.UtcNow;
    }
}
