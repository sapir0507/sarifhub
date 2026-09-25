using SarifHub.Domain.Common;

namespace SarifHub.Domain.Findings;

/// <summary>Where and how a finding was reported in one scan.</summary>
public sealed record FindingLocation
{
    /// <summary>Creates a location; line and column numbers are 1-based.</summary>
    public FindingLocation(string filePath, int? startLine, int? startColumn, int? endLine, string message, string? snippet)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (startLine <= 0 || startColumn <= 0)
        {
            throw new DomainException("Line and column numbers start at 1.");
        }

        if (endLine < startLine)
        {
            throw new DomainException("A location cannot end before it starts.");
        }

        FilePath = filePath;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        Message = message;
        Snippet = snippet;
    }

    /// <summary>Repository-relative path.</summary>
    public string FilePath { get; }

    /// <summary>First line, when the tool reports a region.</summary>
    public int? StartLine { get; }

    /// <summary>First column, when reported.</summary>
    public int? StartColumn { get; }

    /// <summary>Last line, when reported.</summary>
    public int? EndLine { get; }

    /// <summary>The tool's message for this result.</summary>
    public string Message { get; }

    /// <summary>Source snippet from the SARIF region, when the tool includes one.</summary>
    public string? Snippet { get; }
}
