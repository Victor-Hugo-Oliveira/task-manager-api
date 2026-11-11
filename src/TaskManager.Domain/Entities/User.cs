namespace TaskManager.Domain.Entities;

public class User
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    /// <summary>Always stored trimmed and lower-case so lookups and the unique index are case-insensitive.</summary>
    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Required by EF Core.
    private User() { }

    public User(string name, string email, string passwordHash)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
