namespace SarifHub.Domain.Scans;

/// <summary>The diff of a scan against the previous one.</summary>
/// <param name="Total">Findings present in the scan.</param>
/// <param name="New">Present for the first time.</param>
/// <param name="Existing">Present in this and the previous scan.</param>
/// <param name="Reopened">Present again after having been resolved.</param>
/// <param name="Resolved">Present in the previous scan, absent from this one.</param>
public sealed record ScanCounts(int Total, int New, int Existing, int Reopened, int Resolved);
