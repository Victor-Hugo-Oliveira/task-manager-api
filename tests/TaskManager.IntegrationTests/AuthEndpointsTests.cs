using System.Net;
using System.Net.Http.Json;
using TaskManager.Application.Auth;
using static TaskManager.IntegrationTests.ApiTestHelpers;

namespace TaskManager.IntegrationTests;

public class AuthEndpointsTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_returns_201_with_token_and_normalized_email()
    {
        var email = UniqueEmail("Alice").ToUpperInvariant();

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Alice", email, DefaultPassword), Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.ReadAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal("Bearer", auth.TokenType);
        Assert.Equal(email.ToLowerInvariant(), auth.User.Email);
        Assert.True(auth.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_with_invalid_data_returns_400_with_field_errors()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("", "not-an-email", "short"), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadProblemAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Name", out _));
        Assert.True(errors.TryGetProperty("Email", out _));
        Assert.True(errors.TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task Register_twice_with_same_email_in_different_case_returns_409()
    {
        var email = UniqueEmail();
        await _client.RegisterAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Other", email.ToUpperInvariant(), DefaultPassword), Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(409, (await response.ReadProblemAsync()).GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_token()
    {
        var email = UniqueEmail();
        var registered = await _client.RegisterAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, DefaultPassword), Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.ReadAsync<AuthResponse>();
        Assert.Equal(registered.User.Id, auth.User.Id);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_return_the_same_401()
    {
        var email = UniqueEmail();
        await _client.RegisterAsync(email);

        var wrongPassword = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPass99"), Json);
        var unknownEmail = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(UniqueEmail(), DefaultPassword), Json);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(
            (await wrongPassword.ReadProblemAsync()).GetProperty("detail").GetString(),
            (await unknownEmail.ReadProblemAsync()).GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-real-token")]
    public async Task Task_endpoints_reject_missing_or_invalid_tokens(string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/tasks");
        if (token is not null) request.Headers.Authorization = new("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
