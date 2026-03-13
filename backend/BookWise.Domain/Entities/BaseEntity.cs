namespace BookWise.Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    public void SoftDelete()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
