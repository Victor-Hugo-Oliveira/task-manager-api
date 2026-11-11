using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>
/// Shared model for every database provider. Each provider has its own subclass
/// (<see cref="SqliteAppDbContext"/>, <see cref="PostgresAppDbContext"/>) so it can keep
/// its own migrations; the rest of the app only depends on this base type.
/// </summary>
public abstract class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    // All dates are stored in UTC. SQLite has no timezone-aware type, so dates come back as
    // DateTimeKind.Unspecified; PostgreSQL's timestamptz requires UTC on write. This handles both.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
