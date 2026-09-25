using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Findings;
using SarifHub.Domain.Scans;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary><c>finding_resolutions</c>: a finding absent from a scan after being present in the previous one.</summary>
internal sealed class FindingResolutionConfiguration : IEntityTypeConfiguration<FindingResolution>
{
    public void Configure(EntityTypeBuilder<FindingResolution> builder)
    {
        builder.ToTable("finding_resolutions");
        builder.HasKey(r => new { r.ScanId, r.FindingId });
        builder.HasOne<Scan>().WithMany().HasForeignKey(r => r.ScanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => r.FindingId).HasDatabaseName("ix_resolutions_finding");
    }
}
