using FluentValidation;
using NSubstitute;
using TaskManager.Application.Auth;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.UnitTests.Services;

public class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        // Fake hasher: "hash" is just a prefix, so tests stay fast and readable.
        _hasher.Hash(Arg.Any<string>()).Returns(ci => "hashed:" + ci.Arg<string>());
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => ci.ArgAt<string>(1) == "hashed:" + ci.ArgAt<string>(0));
        _tokens.CreateToken(Arg.Any<User>()).Returns(new AccessToken("token-123", DateTime.UtcNow.AddHours(1)));

        _service = new AuthService(_users, _hasher, _tokens, new RegisterRequestValidator(), new LoginRequestValidator());
    }

    private User ExistingUser(string email = "alice@example.com", string password = "Password123")
    {
        var user = new User("Alice", email, "hashed:" + password);
        _users.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    [Fact]
    public async Task Register_stores_hashed_password_and_normalized_email()
    {
        var response = await _service.RegisterAsync(new RegisterRequest("Alice", " Alice@Example.com ", "Password123"));

        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "alice@example.com" && u.PasswordHash == "hashed:Password123"),
            Arg.Any<CancellationToken>());
        Assert.Equal("token-123", response.AccessToken);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal("alice@example.com", response.User.Email);
    }

    [Fact]
    public async Task Register_with_existing_email_throws_conflict()
    {
        _users.EmailExistsAsync("alice@example.com", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.RegisterAsync(new RegisterRequest("Alice", "ALICE@example.com", "Password123")));

        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_with_weak_password_throws_validation()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.RegisterAsync(new RegisterRequest("Alice", "alice@example.com", "weak")));
    }

    [Fact]
    public async Task Login_with_correct_credentials_returns_token()
    {
        var user = ExistingUser();

        var response = await _service.LoginAsync(new LoginRequest("Alice@Example.com", "Password123"));

        Assert.Equal("token-123", response.AccessToken);
        Assert.Equal(user.Id, response.User.Id);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_fail_with_the_same_message()
    {
        ExistingUser();

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest("alice@example.com", "WrongPass1")));
        var unknownEmail = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest("nobody@example.com", "Password123")));

        Assert.Equal(wrongPassword.Message, unknownEmail.Message);
    }

    [Fact]
    public async Task Unknown_email_still_runs_a_password_check()
    {
        // Keeps response time similar to a wrong password, so e-mails can't be enumerated by timing.
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest("nobody@example.com", "Password123")));

        _hasher.Received(1).Verify("Password123", Arg.Any<string>());
    }
}
