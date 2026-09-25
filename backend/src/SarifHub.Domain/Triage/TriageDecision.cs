using SarifHub.Domain.Common;

namespace SarifHub.Domain.Triage;

/// <summary>
/// One entry of a finding's triage history. Append-only: decisions are never changed or deleted.
/// The guards mirror the database CHECK constraints on <c>triage_decisions</c>.
/// </summary>
public sealed class TriageDecision
{
    /// <summary>Maximum length of a reason.</summary>
    public const int MaxReasonLength = 1000;

    /// <summary>Minimum length of a reason for every human decision except Confirmed.</summary>
    public const int MinReasonLength = 10;

    private TriageDecision()
    {
        Reason = string.Empty;
    }

    /// <summary>Primary key (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>The finding this decision is about.</summary>
    public Guid FindingId { get; private set; }

    /// <summary>The decided status.</summary>
    public TriageStatus Status { get; private set; }

    /// <summary>Why. Required (at least 10 characters) for human decisions other than Confirmed.</summary>
    public string Reason { get; private set; }

    /// <summary>End of an accepted risk. Set exactly when <see cref="Status"/> is AcceptedRisk.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>The person who decided; <c>null</c> for a system decision (for example, a lapsed acceptance).</summary>
    public Guid? DecidedById { get; private set; }

    /// <summary>When the decision was made (UTC).</summary>
    public DateTimeOffset DecidedAt { get; private set; }

    /// <summary>Creates a decision, enforcing the same rules as the database.</summary>
    public static TriageDecision Create(
        Guid findingId,
        TriageStatus status,
        string reason,
        DateTimeOffset? expiresAt,
        Guid? decidedById,
        DateTimeOffset decidedAt)
    {
        ArgumentNullException.ThrowIfNull(reason);
        var trimmed = reason.Trim();

        if (trimmed.Length > MaxReasonLength)
        {
            throw new DomainException($"A triage reason is at most {MaxReasonLength} characters.");
        }

        if (decidedById is not null && status != TriageStatus.Confirmed && trimmed.Length < MinReasonLength)
        {
            throw new DomainException($"A {status} decision needs a reason of at least {MinReasonLength} characters.");
        }

        if ((status == TriageStatus.AcceptedRisk) != expiresAt.HasValue)
        {
            throw new DomainException("An expiry date is required for an accepted risk and not allowed otherwise.");
        }

        return new TriageDecision
        {
            Id = Guid.CreateVersion7(decidedAt),
            FindingId = findingId,
            Status = status,
            Reason = trimmed,
            ExpiresAt = expiresAt,
            DecidedById = decidedById,
            DecidedAt = decidedAt,
        };
    }
}
