namespace SarifHub.Domain.Triage;

/// <summary>A person's decision about a finding. Independent of the finding's lifecycle.</summary>
public enum TriageStatus
{
    /// <summary>No decision yet.</summary>
    Untriaged = 1,

    /// <summary>The finding is real and should be fixed.</summary>
    Confirmed = 2,

    /// <summary>The tool is wrong here. Excluded from active counts and the quality gate.</summary>
    FalsePositive = 3,

    /// <summary>Real, but accepted until an expiry date. Excluded from the gate until then.</summary>
    AcceptedRisk = 4,
}
