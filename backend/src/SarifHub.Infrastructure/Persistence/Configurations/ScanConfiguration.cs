using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Gate;
using SarifHub.Domain.Projects;
using SarifHub.Domain.Scans;
using SarifHub.Domain.Users;
using SarifHub.Infrastructure.Persistence.Json;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary><c>scans</c>: immutable snapshots with diff counts, severity counts and the gate result (ADR 0008).</summary>
internal sealed class ScanConfiguration : IEntityTypeConfiguration<Scan>
{
    public void Configure(EntityTypeBuilder<Scan> builder)
    {
        builder.ToTable("scans", t =>
        {
            t.HasCheckConstraint("ck_scans_number_positive", "number > 0");
            t.HasCheckConstraint("ck_scans_commit_sha_format", "commit_sha ~ '^[0-9a-f]{7,64}$'");
            t.HasCheckConstraint("ck_scans_status", Checks.In<ScanStatus>("status"));
            t.HasCheckConstraint("ck_scans_gate_result", Checks.In<GateResult>("gate_result"));
            t.HasCheckConstraint("ck_scans_one_uploader", "num_nonnulls(uploaded_by_user_id, uploaded_by_key_id) = 1");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Status).HasConversion<string>();
        builder.Property(s => s.GateResult).HasConversion<string>();
        builder.Property(s => s.GateEvaluation)
            .HasConversion(GateEvaluationJson.Converter, GateEvaluationJson.Comparer)
            .HasColumnType("jsonb");

        builder.Ignore(s => s.SarifSha256);
        builder.Property<byte[]>("_sarifSha256").HasColumnName("sarif_sha256").IsRequired();
        builder.Ignore(s => s.Diff);
        builder.Ignore(s => s.PresentBySeverity);
        builder.Ignore(s => s.IsCompleted);

        foreach (var count in new[]
        {
            nameof(Scan.TotalCount), nameof(Scan.NewCount), nameof(Scan.ExistingCount), nameof(Scan.ReopenedCount),
            nameof(Scan.ResolvedCount), nameof(Scan.CriticalCount), nameof(Scan.HighCount), nameof(Scan.MediumCount),
            nameof(Scan.LowCount),
        })
        {
            builder.Property<int>(count).HasDefaultValue(0);
        }

        builder.HasOne<Project>().WithMany().HasForeignKey(s => s.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UploadedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ApiKey>().WithMany().HasForeignKey(s => s.UploadedByKeyId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(s => new { s.ProjectId, s.Number }).IsUnique();
        builder.HasIndex(s => new { s.ProjectId, s.UploadedAt })
            .HasDatabaseName("ix_scans_project_uploaded")
            .IsDescending(false, true);
        // Detects a re-upload of an identical file (CI retries are safe).
        builder.HasIndex("ProjectId", "_sarifSha256").HasDatabaseName("ix_scans_project_sha");

        builder.HasMany(s => s.Tools).WithOne().HasForeignKey(t => t.ScanId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Tools).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
