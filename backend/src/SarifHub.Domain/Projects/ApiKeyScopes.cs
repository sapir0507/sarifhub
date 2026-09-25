namespace SarifHub.Domain.Projects;

/// <summary>What a CI API key may do. Nothing else is possible with a key.</summary>
public static class ApiKeyScopes
{
    /// <summary>Upload SARIF files.</summary>
    public const string ScansWrite = "scans:write";

    /// <summary>Read quality gate results.</summary>
    public const string GateRead = "gate:read";

    /// <summary>Every known scope.</summary>
    public static IReadOnlyList<string> All { get; } = [ScansWrite, GateRead];
}
