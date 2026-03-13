namespace BookWise.Domain.Entities;

public class UserAccount : BaseEntity
{
    public string? Email { get; private set; }
    public string? Name { get; private set; }
    public string? GoogleSubject { get; private set; }
    public string? PhoneNumberE164 { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private UserAccount() { }

    public static UserAccount CreateFromPhone(string phoneNumberE164)
    {
        return new UserAccount
        {
            PhoneNumberE164 = phoneNumberE164,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public static UserAccount CreateFromGoogle(string googleSubject, string? email, string? name)
    {
        return new UserAccount
        {
            GoogleSubject = googleSubject,
            Email = email,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void UpdateGoogleProfile(string? email, string? name)
    {
        Email = email;
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
