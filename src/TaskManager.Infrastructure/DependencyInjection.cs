using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Interfaces;
using TaskManager.Infrastructure.Authentication;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Repositories;

namespace TaskManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // "Database:Provider" = Sqlite (default) | Postgres
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        switch (provider.ToLowerInvariant())
        {
            case "sqlite":
                services.AddDbContext<SqliteAppDbContext>(o => o.UseSqlite(connectionString));
                services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<SqliteAppDbContext>());
                break;
            case "postgres":
                services.AddDbContext<PostgresAppDbContext>(o => o.UseNpgsql(connectionString));
                services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<PostgresAppDbContext>());
                break;
            default:
                throw new InvalidOperationException($"Unsupported Database:Provider '{provider}'. Use 'Sqlite' or 'Postgres'.");
        }

        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Fail at startup (not at first login) if the JWT config is missing or weak.
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(s => s.Secret.Length >= 32, "Jwt:Secret must be at least 32 characters.")
            .Validate(s => s.ExpirationMinutes > 0, "Jwt:ExpirationMinutes must be positive.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
