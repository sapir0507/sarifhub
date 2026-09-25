using System.Net;

namespace SarifHub.Domain.Audit;

/// <summary>
/// An append-only record of a security-relevant action. Written from Phase 5 on (uploads, triage, key changes).
/// <see cref="Data"/> is a JSON object that must never contain secrets or raw keys.
/// </summary>
public sealed class AuditEvent
{
    private AuditEvent()
    {
        Action = string.Empty;
        Data = "{}";
    }

    /// <summary>Database-generated sequence number.</summary>
    public long Id { get; private set; }

    /// <summary>When the action happened (UTC).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Kind of actor.</summary>
    public AuditActorType ActorType { get; private set; }

    /// <summary>User or API key id, when known.</summary>
    public Guid? ActorId { get; private set; }

    /// <summary>Project concerned; kept as <c>null</c> after the project is deleted.</summary>
    public Guid? ProjectId { get; private set; }

    /// <summary>Action name, e.g. <c>scan.uploaded</c>, <c>finding.triaged</c>.</summary>
    public string Action { get; private set; }

    /// <summary>Kind of object acted on.</summary>
    public string? TargetType { get; private set; }

    /// <summary>Id of the object acted on.</summary>
    public string? TargetId { get; private set; }

    /// <summary>Client address, when the action came from a request.</summary>
    public IPAddress? IpAddress { get; private set; }

    /// <summary>Extra details as a JSON object. Never secrets.</summary>
    public string Data { get; private set; }

    /// <summary>Creates an event.</summary>
    public static AuditEvent Record(
        DateTimeOffset occurredAt,
        AuditActorType actorType,
        Guid? actorId,
        Guid? projectId,
        string action,
        string? targetType = null,
        string? targetId = null,
        IPAddress? ipAddress = null,
        string data = "{}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(data);
        return new AuditEvent
        {
            OccurredAt = occurredAt,
            ActorType = actorType,
            ActorId = actorId,
            ProjectId = projectId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            IpAddress = ipAddress,
            Data = data,
        };
    }
}
