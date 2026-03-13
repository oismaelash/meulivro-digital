namespace BookWise.Domain.Entities;

public class LoginOtp : BaseEntity
{
    public string PhoneNumberE164 { get; private set; } = null!;
    public string CodeHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? PilotMessageId { get; private set; }

    private LoginOtp() { }

    public static LoginOtp Create(string phoneNumberE164, string codeHash, DateTime expiresAt, string? pilotMessageId)
    {
        return new LoginOtp
        {
            PhoneNumberE164 = phoneNumberE164,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            PilotMessageId = pilotMessageId,
            Attempts = 0,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public bool IsValidNow(DateTime nowUtc) =>
        ConsumedAt is null && nowUtc <= ExpiresAt && IsActive;

    public void MarkAttempt()
    {
        Attempts++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Consume()
    {
        ConsumedAt = DateTime.UtcNow;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
