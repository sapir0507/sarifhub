using SarifHub.Domain.Common;

namespace SarifHub.Domain.Users;

/// <summary>
/// A person who can be a project member and make triage decisions.
/// Credentials (password hash, security stamp, lockout) are added by ASP.NET Core Identity in Phase 6 and are
/// deliberately not part of the domain model.
/// </summary>
public sealed class User
{
    private User()
    {
        Email = string.Empty;
        NormalizedEmail = string.Empty;
        DisplayName = string.Empty;
    }

    /// <summary>Primary key (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Email address as entered.</summary>
    public string Email { get; private set; }

    /// <summary>Upper-cased email, unique; used for lookups.</summary>
    public string NormalizedEmail { get; private set; }

    /// <summary>Name shown in the UI and in triage history.</summary>
    public string DisplayName { get; private set; }

    /// <summary>When the user was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Normalizes an email address the way lookups expect.</summary>
    public static string Normalize(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToUpperInvariant();
    }

    /// <summary>Creates a user.</summary>
    public static User Create(string email, string displayName, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        var name = displayName.Trim();
        if (name.Length > 100)
        {
            throw new DomainException("A display name is at most 100 characters.");
        }

        return new User
        {
            Id = Guid.CreateVersion7(createdAt),
            Email = email.Trim(),
            NormalizedEmail = Normalize(email),
            DisplayName = name,
            CreatedAt = createdAt,
        };
    }
}
