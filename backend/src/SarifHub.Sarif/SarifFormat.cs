namespace SarifHub.Sarif;

/// <summary>
/// The SARIF version SarifHub reads. The reader and normalizer that turn SARIF files into SarifHub's model arrive
/// in Phase 4; this project exists now so the dependency graph is in place from the start (ADR 0002).
/// </summary>
public static class SarifFormat
{
    /// <summary>Supported SARIF version.</summary>
    public const string Version = "2.1.0";

    /// <summary>Media type registered for SARIF files.</summary>
    public const string MediaType = "application/sarif+json";
}
