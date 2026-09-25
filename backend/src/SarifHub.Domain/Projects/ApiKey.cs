using System.Text.RegularExpressions;
using SarifHub.Domain.Common;

namespace SarifHub.Domain.Projects;

/// <summary>
/// A project-scoped credential for CI. The key is shown once as <c>shk_&lt;prefix&gt;_&lt;secret&gt;</c>;
/// only the public prefix and a SHA-256 hash are ever stored (ADR 0007). Key generation and verification are Phase 6.
/// </summary>
public sealed partial class ApiKey
{
    /// <summary>Length of the SHA-256 hash in bytes.</summary>
    public const int HashLength = 32;

    private byte[] _keyHash = [];
    private string[] _scopes = [];

    private ApiKey()
    {
        Name = string.Empty;
        Prefix = string.Empty;
    }

    /// <summary>Primary key (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>The project the key is bound to.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Human name, unique per project, e.g. <c>ci-main</c>.</summary>
    public string Name { get; private set; }

    /// <summary>Public, indexed part of the key used to find the row (8 characters).</summary>
    public string Prefix { get; private set; }

    /// <summary>SHA-256 of the full key. The key itself is never stored.</summary>
    public IReadOnlyList<byte> KeyHash => _keyHash;

    /// <summary>Granted scopes (<see cref="ApiKeyScopes"/>).</summary>
    public IReadOnlyList<string> Scopes => _scopes;

    /// <summary>When the key was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The Admin who created the key.</summary>
    public Guid CreatedById { get; private set; }

    /// <summary>Optional expiry.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>Last successful use.</summary>
    public DateTimeOffset? LastUsedAt { get; private set; }

    /// <summary>When the key was revoked. Revoked keys are kept for the audit trail.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Creates a key record from an already computed hash.</summary>
    public static ApiKey Create(
        Guid projectId,
        string name,
        string prefix,
        IReadOnlyList<byte> keyHash,
        IReadOnlyList<string> scopes,
        Guid createdById,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(keyHash);
        ArgumentNullException.ThrowIfNull(scopes);

        if (name.Trim().Length > 60)
        {
            throw new DomainException("An API key name is at most 60 characters.");
        }

        if (!PrefixPattern().IsMatch(prefix))
        {
            throw new DomainException("An API key prefix is 8 lowercase letters or digits.");
        }

        if (keyHash.Count != HashLength)
        {
            throw new DomainException("An API key hash must be a SHA-256 value (32 bytes).");
        }

        if (scopes.Count == 0 || scopes.Any(s => !ApiKeyScopes.All.Contains(s)))
        {
            throw new DomainException("An API key needs at least one scope, and only known scopes.");
        }

        return new ApiKey
        {
            Id = Guid.CreateVersion7(createdAt),
            ProjectId = projectId,
            Name = name.Trim(),
            Prefix = prefix,
            _keyHash = [.. keyHash],
            _scopes = [.. scopes.Distinct(StringComparer.Ordinal)],
            CreatedById = createdById,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
        };
    }

    /// <summary>Whether the key can authenticate at <paramref name="now"/>.</summary>
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && (ExpiresAt is null || ExpiresAt > now);

    /// <summary>Revokes the key. Idempotent.</summary>
    public void Revoke(DateTimeOffset at) => RevokedAt ??= at;

    [GeneratedRegex("^[a-z0-9]{8}$", RegexOptions.CultureInvariant)]
    private static partial Regex PrefixPattern();
}
