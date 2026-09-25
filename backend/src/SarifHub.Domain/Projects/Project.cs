using System.Text.RegularExpressions;
using SarifHub.Domain.Common;
using SarifHub.Domain.Gate;

namespace SarifHub.Domain.Projects;

/// <summary>
/// A code repository whose scans are tracked. Owns its members, its quality gate policy and the scan number sequence.
/// </summary>
public sealed partial class Project
{
    private readonly List<ProjectMember> _members = [];

    private Project()
    {
        Key = string.Empty;
        Name = string.Empty;
        DefaultBranch = string.Empty;
        GatePolicy = GatePolicy.Default;
    }

    /// <summary>Primary key (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>URL-safe, unique key used in CI URLs, e.g. <c>payments-api</c>.</summary>
    public string Key { get; private set; }

    /// <summary>Display name.</summary>
    public string Name { get; private set; }

    /// <summary>HTTPS URL of the repository, if known.</summary>
    public Uri? RepositoryUrl { get; private set; }

    /// <summary>Branch used when an upload does not name one.</summary>
    public string DefaultBranch { get; private set; }

    /// <summary>Quality gate thresholds.</summary>
    public GatePolicy GatePolicy { get; private set; }

    /// <summary>Number of the latest scan; 0 before the first upload.</summary>
    public int LastScanNumber { get; private set; }

    /// <summary>When the project was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The user who created the project.</summary>
    public Guid CreatedById { get; private set; }

    /// <summary>Row version for optimistic concurrency (PostgreSQL <c>xmin</c>).</summary>
    public uint Version { get; private set; }

    /// <summary>Members and their roles.</summary>
    public IReadOnlyList<ProjectMember> Members => _members;

    /// <summary>Creates a project; the creator becomes its first Admin.</summary>
    public static Project Create(
        string key,
        string name,
        Uri? repositoryUrl,
        string defaultBranch,
        GatePolicy gatePolicy,
        Guid createdById,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultBranch);
        ArgumentNullException.ThrowIfNull(gatePolicy);

        if (!KeyPattern().IsMatch(key))
        {
            throw new DomainException($"'{key}' is not a valid project key: use 2–63 lowercase letters, digits and hyphens.");
        }

        if (name.Trim().Length > 100)
        {
            throw new DomainException("A project name is at most 100 characters.");
        }

        if (repositoryUrl is not null && repositoryUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new DomainException("A repository URL must use https.");
        }

        var project = new Project
        {
            Id = Guid.CreateVersion7(createdAt),
            Key = key,
            Name = name.Trim(),
            RepositoryUrl = repositoryUrl,
            DefaultBranch = defaultBranch.Trim(),
            GatePolicy = gatePolicy,
            CreatedById = createdById,
            CreatedAt = createdAt,
        };
        project.AddMember(createdById, ProjectRole.Admin, createdAt);
        return project;
    }

    /// <summary>Adds a member. A user can be a member once.</summary>
    public void AddMember(Guid userId, ProjectRole role, DateTimeOffset addedAt)
    {
        if (_members.Exists(m => m.UserId == userId))
        {
            throw new DomainException("The user is already a member of this project.");
        }

        _members.Add(new ProjectMember(Id, userId, role, addedAt));
    }

    /// <summary>The role of a user, or <c>null</c> when the user is not a member.</summary>
    public ProjectRole? RoleOf(Guid userId) => _members.Find(m => m.UserId == userId)?.Role;

    /// <summary>
    /// Reserves the next scan number. Called by ingestion while it holds the project's ingestion lock (ADR 0006),
    /// which is what keeps numbers gap-free and unique.
    /// </summary>
    public int AllocateScanNumber() => ++LastScanNumber;

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,62}$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}
