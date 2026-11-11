using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.IntegrationTests;

/// <summary>
/// Boots the real API in memory against a private in-memory SQLite database.
/// The real migrations are applied, so they are tested too.
/// </summary>
public class TaskManagerApiFactory : WebApplicationFactory<Program>
{
    // A shared-cache in-memory database lives as long as at least one connection is open.
    private readonly SqliteConnection _keepAlive =
        new($"Data Source=tm-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _keepAlive.Open();

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _keepAlive.ConnectionString);
        builder.UseSetting("Jwt:Secret", "integration-tests-only-secret-0123456789abcdef");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _keepAlive.Dispose();
    }
}
