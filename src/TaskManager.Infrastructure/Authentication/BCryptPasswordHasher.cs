using TaskManager.Application.Interfaces;

namespace TaskManager.Infrastructure.Authentication;

public class BCryptPasswordHasher : IPasswordHasher
{
    // Cost 12 ≈ a few hundred ms per hash: slow for brute force, fine for a login request.
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.EnhancedHashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
}
