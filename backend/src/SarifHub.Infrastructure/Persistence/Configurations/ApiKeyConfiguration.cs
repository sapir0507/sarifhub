using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Projects;
using SarifHub.Domain.Users;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>api_keys</c>: the public prefix is indexed for lookup; the key itself is stored only as a SHA-256 hash (ADR 0007).
/// </summary>
internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys", t =>
        {
            t.HasCheckConstraint("ck_api_keys_name_length", "length(name) BETWEEN 1 AND 60");
            t.HasCheckConstraint("ck_api_keys_key_hash_sha256", "octet_length(key_hash) = 32");
            t.HasCheckConstraint(
                "ck_api_keys_scopes",
                $"scopes <@ ARRAY[{string.Join(", ", ApiKeyScopes.All.Select(s => $"'{s}'"))}]::text[] AND cardinality(scopes) > 0");
        });

        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).ValueGeneratedNever();
        builder.Property(k => k.Prefix).HasColumnType("character(8)");
        builder.HasIndex(k => k.Prefix).IsUnique();
        builder.HasIndex(k => new { k.ProjectId, k.Name }).IsUnique();

        builder.Ignore(k => k.KeyHash);
        builder.Property<byte[]>("_keyHash").HasColumnName("key_hash").IsRequired();
        builder.Ignore(k => k.Scopes);
        builder.Property<string[]>("_scopes").HasColumnName("scopes").IsRequired();

        builder.HasOne<Project>().WithMany().HasForeignKey(k => k.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(k => k.CreatedById).OnDelete(DeleteBehavior.NoAction);
    }
}
