using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Scans;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary><c>scan_tools</c>: one row per tool run in the uploaded file.</summary>
internal sealed class ScanToolConfiguration : IEntityTypeConfiguration<ScanTool>
{
    public void Configure(EntityTypeBuilder<ScanTool> builder)
    {
        builder.ToTable("scan_tools");
        builder.HasKey(t => new { t.ScanId, t.ToolName });
    }
}
