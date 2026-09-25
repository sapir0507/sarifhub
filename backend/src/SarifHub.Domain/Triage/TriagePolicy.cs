using SarifHub.Domain.Projects;

namespace SarifHub.Domain.Triage;

/// <summary>
/// Which project role may make which triage decision — the same matrix as
/// <c>frontend/src/domain/permissions.ts</c>. False positive and accepted risk both remove a finding from the
/// quality gate, so both need SecurityLead or Admin (ADR 0009).
/// Enforced by the triage use case in Phase 6; defined here so the rule has a single owner.
/// </summary>
public static class TriagePolicy
{
    private static readonly TriageStatus[] DeveloperDecisions = [TriageStatus.Confirmed, TriageStatus.Untriaged];

    private static readonly TriageStatus[] SecurityLeadDecisions =
        [TriageStatus.Confirmed, TriageStatus.Untriaged, TriageStatus.FalsePositive, TriageStatus.AcceptedRisk];

    /// <summary>The decisions a role may make.</summary>
    public static IReadOnlyList<TriageStatus> AllowedDecisions(ProjectRole role) => role switch
    {
        ProjectRole.Viewer => [],
        ProjectRole.Developer => DeveloperDecisions,
        ProjectRole.SecurityLead or ProjectRole.Admin => SecurityLeadDecisions,
        _ => [],
    };

    /// <summary>Whether <paramref name="role"/> may set a finding to <paramref name="status"/>.</summary>
    public static bool CanDecide(ProjectRole role, TriageStatus status) => AllowedDecisions(role).Contains(status);
}
