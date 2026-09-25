namespace SarifHub.Domain.Rules;

/// <summary>
/// A rule as reported by an analysis tool (for example CodeQL <c>cs/sql-injection</c>).
/// Shared across projects; the latest upload's description wins.
/// </summary>
public sealed class Rule
{
    private string[] _cwe = [];

    private Rule()
    {
        ToolName = string.Empty;
        RuleId = string.Empty;
    }

    /// <summary>Primary key (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Tool that defines the rule, e.g. <c>CodeQL</c>.</summary>
    public string ToolName { get; private set; }

    /// <summary>The tool's own rule id, e.g. <c>cs/sql-injection</c>.</summary>
    public string RuleId { get; private set; }

    /// <summary>Human-readable name.</summary>
    public string? Name { get; private set; }

    /// <summary>One-paragraph description.</summary>
    public string? ShortDescription { get; private set; }

    /// <summary>Link to the tool's documentation for the rule.</summary>
    public Uri? HelpUri { get; private set; }

    /// <summary>CWE identifiers, e.g. <c>CWE-89</c>.</summary>
    public IReadOnlyList<string> Cwe => _cwe;

    /// <summary>Creates a rule.</summary>
    public static Rule Create(
        string toolName,
        string ruleId,
        string? name,
        string? shortDescription,
        Uri? helpUri,
        IEnumerable<string> cwe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
        ArgumentNullException.ThrowIfNull(cwe);

        return new Rule
        {
            Id = Guid.CreateVersion7(),
            ToolName = toolName,
            RuleId = ruleId,
            Name = name,
            ShortDescription = shortDescription,
            HelpUri = helpUri,
            _cwe = [.. cwe.Distinct(StringComparer.Ordinal)],
        };
    }
}
