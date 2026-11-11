using TaskManager.Domain.Entities;

namespace TaskManager.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateToken(User user);
}

/// <summary>The authenticated user of the current request.</summary>
public interface ICurrentUser
{
    /// <exception cref="Common.Exceptions.UnauthorizedException">No authenticated user.</exception>
    Guid UserId { get; }
}
