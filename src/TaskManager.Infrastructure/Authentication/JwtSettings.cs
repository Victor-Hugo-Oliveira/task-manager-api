namespace TaskManager.Infrastructure.Authentication;

/// <summary>Bound from the "Jwt" configuration section.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 signing key; at least 32 characters. Never commit a real one — use env var Jwt__Secret.</summary>
    public string Secret { get; init; } = string.Empty;

    public string Issuer { get; init; } = "TaskManager.Api";
    public string Audience { get; init; } = "TaskManager.Client";
    public int ExpirationMinutes { get; init; } = 60;
}
