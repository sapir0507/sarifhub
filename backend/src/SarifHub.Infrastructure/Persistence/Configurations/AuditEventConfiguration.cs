using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Audit;
using SarifHub.Domain.Projects;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>audit_events</c>: append-only. Deleting a project keeps its events with <c>project_id</c> set to NULL:
/// the audit trail must survive the thing it audits.
/// </summary>
internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events", t => t.HasCheckConstraint("ck_audit_events_actor_type", Checks.In<AuditActorType>("actor_type")));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityAlwaysColumn();
        builder.Property(e => e.ActorType).HasConversion<string>();
        builder.Property(e => e.Data).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(e => new { e.ProjectId, e.OccurredAt }).HasDatabaseName("ix_audit_project_time").IsDescending(false, true);
        builder.HasIndex(e => new { e.ActorId, e.OccurredAt }).HasDatabaseName("ix_audit_actor_time").IsDescending(false, true);
    }
}
