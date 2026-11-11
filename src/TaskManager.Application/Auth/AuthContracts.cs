using FluentValidation;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Auth;

public record RegisterRequest(string Name, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record UserResponse(Guid Id, string Name, string Email);

public record AuthResponse(string AccessToken, string TokenType, DateTime ExpiresAt, UserResponse User);

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128; // Upper bound so nobody makes us hash megabytes.

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(User.NameMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress()
            .MaximumLength(User.EmailMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordMinLength)
            .MaximumLength(PasswordMaxLength)
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}
