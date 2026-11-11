using TaskManager.Domain.Entities;

namespace TaskManager.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Email_is_normalized_to_trimmed_lower_case()
    {
        var user = new User("  Alice ", "  Alice@Example.COM ", "hash");

        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public void NormalizeEmail_matches_what_the_entity_stores()
    {
        Assert.Equal("bob@example.com", User.NormalizeEmail(" BOB@example.com"));
    }
}
