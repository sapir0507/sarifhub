using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Users;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>users</c>. Only the columns SarifHub itself relies on; ASP.NET Core Identity adds its credential columns
/// (password hash, security stamp, lockout) in the Phase 6 migration.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", t => t.HasCheckConstraint("ck_users_display_name_length", "length(display_name) BETWEEN 1 AND 100"));
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
    }
}
