using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Common;
using SarifHub.Domain.Findings;
using SarifHub.Domain.Scans;
using SarifHub.Infrastructure.Persistence.Json;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary><c>finding_occurrences</c>: presence of a finding in a scan, with the location as reported then.</summary>
internal sealed class FindingOccurrenceConfiguration : IEntityTypeConfiguration<FindingOccurrence>
{
    public void Configure(EntityTypeBuilder<FindingOccurrence> builder)
    {
        builder.ToTable("finding_occurrences", t =>
        {
            t.HasCheckConstraint("ck_finding_occurrences_change", Checks.In("change", Lifecycle.Resolved));
            t.HasCheckConstraint("ck_finding_occurrences_severity", Checks.In<Severity>("severity"));
            t.HasCheckConstraint("ck_finding_occurrences_start_line_positive", "start_line > 0");
            t.HasCheckConstraint("ck_finding_occurrences_start_column_positive", "start_column > 0");
        });

        builder.HasKey(o => new { o.ScanId, o.FindingId });
        builder.Property(o => o.Change).HasConversion<string>();
        builder.Property(o => o.Severity).HasConversion<string>();
        builder.Property(o => o.PartialFingerprints)
            .HasConversion(StringMapJson.Converter, StringMapJson.Comparer)
            .HasColumnType("jsonb");

        builder.HasOne<Scan>().WithMany().HasForeignKey(o => o.ScanId).OnDelete(DeleteBehavior.Cascade);

        // Finding details: occurrence history of one finding.
        builder.HasIndex(o => new { o.FindingId, o.ScanId }).HasDatabaseName("ix_occurrences_finding");
    }
}
