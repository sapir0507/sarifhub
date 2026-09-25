namespace SarifHub.Domain.Audit;

/// <summary>Who performed an audited action.</summary>
public enum AuditActorType
{
    /// <summary>A signed-in person.</summary>
    User = 1,

    /// <summary>A CI API key.</summary>
    ApiKey = 2,

    /// <summary>SarifHub itself (for example, a lapsed acceptance).</summary>
    System = 3,

    /// <summary>An unauthenticated request (for example, a failed login).</summary>
    Anonymous = 4,
}
