using FluentValidation;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator) : IAuthService
{
    // Same message for "unknown e-mail" and "wrong password" so attackers can't discover registered e-mails.
    private const string InvalidCredentials = "Invalid email or password.";

    // Hash checked when the e-mail doesn't exist, so both failure paths take about the same time.
    private readonly Lazy<string> _dummyHash = new(() => passwordHasher.Hash("dummy-password-for-timing"));

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        await registerValidator.ValidateAndThrowAsync(request, ct);

        var email = User.NormalizeEmail(request.Email);
        if (await users.EmailExistsAsync(email, ct))
            throw new ConflictException("An account with this email already exists.");

        var user = new User(request.Name, email, passwordHasher.Hash(request.Password));
        await users.AddAsync(user, ct);
        await users.SaveChangesAsync(ct);

        return BuildResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);

        var user = await users.GetByEmailAsync(User.NormalizeEmail(request.Email), ct);
        if (user is null)
        {
            passwordHasher.Verify(request.Password, _dummyHash.Value);
            throw new UnauthorizedException(InvalidCredentials);
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException(InvalidCredentials);

        return BuildResponse(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var token = tokenService.CreateToken(user);
        return new AuthResponse(
            token.Token,
            "Bearer",
            token.ExpiresAt,
            new UserResponse(user.Id, user.Name, user.Email));
    }
}
