using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarifHub.Domain.Projects;
using SarifHub.Domain.Users;

namespace SarifHub.Infrastructure.Persistence.Configurations;

/// <summary><c>project_members</c>: one role per user per project.</summary>
internal sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members", t => t.HasCheckConstraint("ck_project_members_role", Checks.In<ProjectRole>("role")));
        builder.HasKey(m => new { m.ProjectId, m.UserId });
        builder.Property(m => m.Role).HasConversion<string>();
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);

        // "My projects" is looked up by user.
        builder.HasIndex(m => m.UserId).HasDatabaseName("ix_project_members_user");
    }
}
