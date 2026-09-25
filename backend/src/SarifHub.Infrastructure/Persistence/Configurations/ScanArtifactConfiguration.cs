using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Scans;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary><c>scan_artifacts</c>: the raw upload, gzip-compressed, one per scan (data model §3.8).</summary>
internal sealed class ScanArtifactConfiguration : IEntityTypeConfiguration<ScanArtifact>
{
    public void Configure(EntityTypeBuilder<ScanArtifact> builder)
    {
        builder.ToTable("scan_artifacts", t => t.HasCheckConstraint("ck_scan_artifacts_original_bytes", "original_bytes > 0"));
        builder.HasKey(a => a.ScanId);
        builder.Ignore(a => a.ContentGzip);
        builder.Property<byte[]>("_contentGzip").HasColumnName("content_gzip").IsRequired();
        builder.HasOne<Scan>().WithOne().HasForeignKey<ScanArtifact>(a => a.ScanId).OnDelete(DeleteBehavior.Cascade);
    }
}
