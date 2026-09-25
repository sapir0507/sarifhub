using SarifHub.Domain.Common;

namespace SarifHub.Infrastructure.Seeding;

/// <summary>A rule the demo data can report, with the places in the code base where it can fire.</summary>
/// <param name="Tool">Tool name.</param>
/// <param name="RuleId">The tool's rule id.</param>
/// <param name="Name">Rule name.</param>
/// <param name="Severity">Normalized severity.</param>
/// <param name="Cwe">CWE identifiers.</param>
/// <param name="Description">Rule description.</param>
/// <param name="Message">Result message.</param>
/// <param name="Files">Files where the rule can fire.</param>
/// <param name="FlaggedLine">The source line the result points at (stored as the SARIF region snippet).</param>
internal sealed record RuleTemplate(
    string Tool,
    string RuleId,
    string Name,
    Severity Severity,
    IReadOnlyList<string> Cwe,
    string Description,
    string Message,
    IReadOnlyList<string> Files,
    string FlaggedLine)
{
    /// <summary>
    /// Link to the rule's documentation. CodeQL ids are real public queries and link to their help pages;
    /// the <c>demo.</c> Semgrep rules are an invented rule pack and have none.
    /// </summary>
    public Uri? HelpUri => Tool == "CodeQL" && RuleId.Split('/') is [var language, _]
        ? new Uri($"https://codeql.github.com/codeql-query-help/{(language == "cs" ? "csharp" : "javascript")}/{RuleId.Replace('/', '-')}/")
        : null;
}
