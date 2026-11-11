using FluentValidation.TestHelper;
using TaskManager.Application.Auth;

namespace TaskManager.UnitTests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest Valid() => new("Alice", "alice@example.com", "Password123");

    [Fact]
    public void Valid_request_passes()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void Email_must_be_valid(string email)
    {
        _validator.TestValidate(Valid() with { Email = email })
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("Short1", "at least 8")]
    [InlineData("onlyletters", "digit")]
    [InlineData("1234567890", "letter")]
    public void Weak_passwords_are_rejected(string password, string expectedMessagePart)
    {
        var result = _validator.TestValidate(Valid() with { Password = password });

        result.ShouldHaveValidationErrorFor(x => x.Password);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RegisterRequest.Password) &&
            e.ErrorMessage.Contains(expectedMessagePart, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Name_is_required()
    {
        _validator.TestValidate(Valid() with { Name = " " })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }
}
