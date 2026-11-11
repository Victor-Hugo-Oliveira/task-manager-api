using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>SQLite: local development and tests. Migrations in Persistence/Migrations/Sqlite.</summary>
public sealed class SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options) : AppDbContext(options);

/// <summary>PostgreSQL: Docker and production. Migrations in Persistence/Migrations/Postgres.</summary>
public sealed class PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options) : AppDbContext(options);

/// <summary>
/// Lets `dotnet ef migrations add --context PostgresAppDbContext` work without a running
/// database or switching the app's provider. Only used at design time.
/// </summary>
public sealed class PostgresDesignTimeFactory : IDesignTimeDbContextFactory<PostgresAppDbContext>
{
    public PostgresAppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PostgresAppDbContext>()
            .UseNpgsql("Host=localhost;Database=taskmanager_design;Username=postgres;Password=postgres")
            .Options);
}
