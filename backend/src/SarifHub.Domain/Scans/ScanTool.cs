namespace SarifHub.Domain.Scans;

/// <summary>One tool run inside an uploaded SARIF file (a file can contain several runs).</summary>
public sealed class ScanTool
{
    private ScanTool()
    {
        ToolName = string.Empty;
    }

    internal ScanTool(Guid scanId, string toolName, string? toolVersion, int resultCount)
    {
        ScanId = scanId;
        ToolName = toolName;
        ToolVersion = toolVersion;
        ResultCount = resultCount;
    }

    /// <summary>The scan.</summary>
    public Guid ScanId { get; private set; }

    /// <summary>Tool name, e.g. <c>CodeQL</c>.</summary>
    public string ToolName { get; private set; }

    /// <summary>Tool version, when reported.</summary>
    public string? ToolVersion { get; private set; }

    /// <summary>Results the tool reported in this run.</summary>
    public int ResultCount { get; private set; }
}
