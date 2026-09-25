using Microsoft.EntityFrameworkCore;
using SarifHub.Domain.Audit;
using SarifHub.Domain.Findings;
using SarifHub.Domain.Projects;
using SarifHub.Domain.Rules;
using SarifHub.Domain.Scans;
using SarifHub.Domain.Triage;
using SarifHub.Domain.Users;

namespace SarifHub.Infrastructure.Persistence;

/// <summary>
/// The EF Core unit of work. Owns the schema (migrations), all writes, and simple reads by id.
/// Report-shaped reads use Dapper over the same <c>NpgsqlDataSource</c> (ADR 0003, see <c>ReadModels/</c>).
/// </summary>
public sealed class SarifHubDbContext(DbContextOptions<SarifHubDbContext> options) : DbContext(options)
{
    /// <summary>People.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Projects.</summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>Project memberships.</summary>
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    /// <summary>CI API keys (hashes only).</summary>
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    /// <summary>Scans.</summary>
    public DbSet<Scan> Scans => Set<Scan>();

    /// <summary>Tool runs per scan.</summary>
    public DbSet<ScanTool> ScanTools => Set<ScanTool>();

    /// <summary>Raw uploads.</summary>
    public DbSet<ScanArtifact> ScanArtifacts => Set<ScanArtifact>();

    /// <summary>Tool rules.</summary>
    public DbSet<Rule> Rules => Set<Rule>();

    /// <summary>Logical findings.</summary>
    public DbSet<Finding> Findings => Set<Finding>();

    /// <summary>Finding presence per scan.</summary>
    public DbSet<FindingOccurrence> FindingOccurrences => Set<FindingOccurrence>();

    /// <summary>Finding disappearance per scan.</summary>
    public DbSet<FindingResolution> FindingResolutions => Set<FindingResolution>();

    /// <summary>Triage history.</summary>
    public DbSet<TriageDecision> TriageDecisions => Set<TriageDecision>();

    /// <summary>Audit trail.</summary>
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Trigram indexes make "contains" search on file paths and messages index-assisted (data model §4).
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SarifHubDbContext).Assembly);
    }
}
