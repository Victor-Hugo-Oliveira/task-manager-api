using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManager.Application.Auth;
using TaskManager.Application.Tasks;

namespace TaskManager.IntegrationTests;

public static class ApiTestHelpers
{
    /// <summary>Same JSON shape the API uses: camelCase and enums as names.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public const string DefaultPassword = "Password123";

    public static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@example.com";

    public static async Task<AuthResponse> RegisterAsync(this HttpClient client, string? email = null, string name = "Test User")
    {
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(name, email ?? UniqueEmail(), DefaultPassword), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    /// <summary>A client authenticated as a brand-new user.</summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this TaskManagerApiFactory factory)
    {
        var client = factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    public static async Task<TaskResponse> CreateTaskAsync(this HttpClient client, CreateTaskRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    /// <summary>Reads a ProblemDetails body as raw JSON (status, title, errors...).</summary>
    public static async Task<JsonElement> ReadProblemAsync(this HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }
}
