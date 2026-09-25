using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Common;
using SarifHub.Domain.Findings;
using SarifHub.Domain.Projects;
using SarifHub.Domain.Rules;
using SarifHub.Domain.Scans;
using SarifHub.Domain.Triage;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>findings</c>: current state denormalized from the history tables (ADR 0004), indexed for the grid.
/// </summary>
internal sealed class FindingConfiguration : IEntityTypeConfiguration<Finding>
{
    /// <summary>Name of the stored generated column that makes severity sortable by an index (data model §3.5).</summary>
    public const string SeverityRank = "SeverityRank";

    public void Configure(EntityTypeBuilder<Finding> builder)
    {
        builder.ToTable("findings", t =>
        {
            t.HasCheckConstraint("ck_findings_severity", Checks.In<Severity>("severity"));
            t.HasCheckConstraint("ck_findings_lifecycle", Checks.In<Lifecycle>("lifecycle"));
            t.HasCheckConstraint("ck_findings_triage_status", Checks.In<TriageStatus>("triage_status"));
            t.HasCheckConstraint("ck_findings_start_line_positive", "start_line > 0");
            t.HasCheckConstraint("ck_findings_accepted_risk_expiry", "(triage_status = 'AcceptedRisk') = (accepted_risk_expires_at IS NOT NULL)");
        });

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.Severity).HasConversion<string>();
        builder.Property(f => f.Lifecycle).HasConversion<string>();
        // DEFAULT 'Untriaged' matches schema.sql. The domain always sets a status; 0 is not a valid value (it is the sentinel).
        builder.Property(f => f.TriageStatus)
            .HasConversion<string>()
            .HasDefaultValue(TriageStatus.Untriaged)
            .HasSentinel(default(TriageStatus));

        // Text sorts Critical, High, Low, Medium; a stored rank gives the default grid order an index to walk.
        builder.Property<short>(SeverityRank)
            .HasComputedColumnSql("CASE severity WHEN 'Critical' THEN 4 WHEN 'High' THEN 3 WHEN 'Medium' THEN 2 ELSE 1 END", stored: true);

        // Triage uses optimistic concurrency (If-Match / ETag, Phase 6); xmin is the row version.
        builder.Property(f => f.Version).IsRowVersion().HasColumnName("xmin").HasColumnType("xid");

        builder.HasOne<Project>().WithMany().HasForeignKey(f => f.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Rule>().WithMany().HasForeignKey(f => f.RuleId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Scan>().WithMany().HasForeignKey(f => f.FirstSeenScanId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Scan>().WithMany().HasForeignKey(f => f.LastSeenScanId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(f => new { f.ProjectId, f.Fingerprint }).IsUnique();

        // Grid default: open findings of a project, most severe first, paged. The id makes the order total.
        builder.HasIndex(nameof(Finding.ProjectId), SeverityRank, nameof(Finding.Id))
            .HasDatabaseName("ix_findings_open_severity")
            .IsDescending(false, true, false)
            .HasFilter("lifecycle <> 'Resolved'");
        builder.HasIndex(f => new { f.ProjectId, f.LastSeenAt })
            .HasDatabaseName("ix_findings_project_last_seen")
            .IsDescending(false, true);
        builder.HasIndex(f => f.RuleId).HasDatabaseName("ix_findings_rule");

        // "Contains" search on path and message (ILIKE '%…%') uses trigram GIN indexes.
        builder.HasIndex(f => f.FilePath).HasDatabaseName("ix_findings_file_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(f => f.Message).HasDatabaseName("ix_findings_message_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");

        // A background job (later phase) finds accepted risks that are about to lapse.
        builder.HasIndex(f => f.AcceptedRiskExpiresAt)
            .HasDatabaseName("ix_findings_risk_expiry")
            .HasFilter("triage_status = 'AcceptedRisk'");

        builder.HasMany(f => f.Occurrences).WithOne().HasForeignKey(o => o.FindingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(f => f.Resolutions).WithOne().HasForeignKey(r => r.FindingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(f => f.TriageHistory).WithOne().HasForeignKey(d => d.FindingId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(f => f.Occurrences).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(f => f.Resolutions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(f => f.TriageHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
