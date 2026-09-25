using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Projects;
using SarifHub.Domain.Users;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary><c>projects</c>: key rules, gate policy columns, optimistic concurrency.</summary>
internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", t =>
        {
            // The key appears in CI URLs, so the database guarantees it is URL-safe.
            t.HasCheckConstraint("ck_projects_key_format", "key ~ '^[a-z0-9][a-z0-9-]{1,62}$'");
            t.HasCheckConstraint("ck_projects_name_length", "length(name) BETWEEN 1 AND 100");
            t.HasCheckConstraint("ck_projects_repository_url_https", "repository_url ~ '^https://'");
            t.HasCheckConstraint("ck_projects_gate_max_critical", "gate_max_critical >= 0");
            t.HasCheckConstraint("ck_projects_gate_max_high", "gate_max_high >= 0");
            t.HasCheckConstraint("ck_projects_gate_max_medium", "gate_max_medium >= 0");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.HasIndex(p => p.Key).IsUnique();
        builder.Property(p => p.RepositoryUrl).HasConversion<string>();
        builder.Property(p => p.DefaultBranch).HasDefaultValue("main");
        builder.Property(p => p.LastScanNumber).HasDefaultValue(0);

        builder.ComplexProperty(p => p.GatePolicy, gate =>
        {
            gate.Property(g => g.MaxCritical).HasColumnName("gate_max_critical");
            gate.Property(g => g.MaxHigh).HasColumnName("gate_max_high");
            gate.Property(g => g.MaxMedium).HasColumnName("gate_max_medium");
        });

        // PostgreSQL's xmin system column changes on every update: a free, always-correct row version.
        builder.Property(p => p.Version).IsRowVersion().HasColumnName("xmin").HasColumnType("xid");

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.CreatedById).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(p => p.Members).WithOne().HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
