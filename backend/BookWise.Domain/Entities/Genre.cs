namespace BookWise.Domain.Entities;

public class Genre : BaseEntity
{
    public int UserAccountId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public virtual ICollection<Book> Books { get; private set; } = new List<Book>();

    protected Genre() { }

    public Genre(int userAccountId, string name, string? description)
    {
        UserAccountId = userAccountId;
        Name = name;
        Description = description;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
