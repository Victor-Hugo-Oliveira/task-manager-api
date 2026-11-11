using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).IsRequired().HasMaxLength(User.NameMaxLength);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(User.EmailMaxLength);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(100);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasMany<TaskItem>()
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
