using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Triage;
using SarifHub.Domain.Users;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary><c>triage_decisions</c>: append-only triage history. The CHECKs mirror <see cref="TriageDecision.Create"/>.</summary>
internal sealed class TriageDecisionConfiguration : IEntityTypeConfiguration<TriageDecision>
{
    public void Configure(EntityTypeBuilder<TriageDecision> builder)
    {
        builder.ToTable("triage_decisions", t =>
        {
            t.HasCheckConstraint("ck_triage_decisions_status", Checks.In<TriageStatus>("status"));
            t.HasCheckConstraint("ck_triage_decisions_reason_length", $"length(reason) <= {TriageDecision.MaxReasonLength}");
            t.HasCheckConstraint("ck_triage_decisions_accepted_risk_expiry", "(status = 'AcceptedRisk') = (expires_at IS NOT NULL)");
            t.HasCheckConstraint(
                "ck_triage_decisions_reason_required",
                $"status = 'Confirmed' OR decided_by_id IS NULL OR length(reason) >= {TriageDecision.MinReasonLength}");
        });

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Status).HasConversion<string>();
        builder.HasOne<User>().WithMany().HasForeignKey(d => d.DecidedById).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(d => new { d.FindingId, d.DecidedAt })
            .HasDatabaseName("ix_triage_finding")
            .IsDescending(false, true);
    }
}
