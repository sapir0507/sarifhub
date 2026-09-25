using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Rules;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary><c>rules</c>: rule metadata shared across projects, unique per tool and rule id.</summary>
internal sealed class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.ToTable("rules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.HelpUri).HasConversion<string>();
        builder.Ignore(r => r.Cwe);
        builder.Property<string[]>("_cwe").HasColumnName("cwe").IsRequired().HasDefaultValueSql("'{}'::text[]");
        builder.HasIndex(r => new { r.ToolName, r.RuleId }).IsUnique();
    }
}
