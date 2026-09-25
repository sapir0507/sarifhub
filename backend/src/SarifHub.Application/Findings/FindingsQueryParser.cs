using System.Collections.Frozen;
using SarifHub.Domain.Common;
using SarifHub.Domain.Findings;
using SarifHub.Domain.Triage;

namespace SarifHub.Application.Findings;

/// <summary>Raw query string values of <c>GET /projects/{id}/findings</c>, as named in the API contract.</summary>
public sealed record FindingsQueryInput(
    int? Page,
    int? PageSize,
    string? Sort,
    string? Dir,
    string? Severity,
    string? Status,
    string? Triage,
    string? Tool,
    string? Q,
    int? Scan);

/// <summary>
/// Turns raw query values into a <see cref="FindingsQuery"/> or a list of validation errors keyed by parameter name.
/// Enum values are matched against explicit allow-lists (names only: numeric values are rejected).
/// </summary>
public static class FindingsQueryParser
{
    /// <summary>Allowed page sizes.</summary>
    public static readonly IReadOnlyList<int> PageSizes = [25, 50, 100];

    /// <summary>Default page size.</summary>
    public const int DefaultPageSize = 25;

    /// <summary>Longest accepted search text.</summary>
    public const int MaxSearchLength = 200;

    private const int MaxPage = 100_000;
    private const int MaxTools = 20;
    private const int MaxToolNameLength = 100;

    private static readonly FrozenDictionary<string, FindingSortField> SortFields = new Dictionary<string, FindingSortField>(StringComparer.OrdinalIgnoreCase)
    {
        ["severity"] = FindingSortField.Severity,
        ["lifecycle"] = FindingSortField.Lifecycle,
        ["triage"] = FindingSortField.Triage,
        ["tool"] = FindingSortField.Tool,
        ["ruleId"] = FindingSortField.RuleId,
        ["filePath"] = FindingSortField.FilePath,
        ["line"] = FindingSortField.Line,
        ["firstSeenAt"] = FindingSortField.FirstSeenAt,
        ["lastSeenAt"] = FindingSortField.LastSeenAt,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>The sortable field names, as clients send them.</summary>
    public static IReadOnlyList<string> SortFieldNames { get; } =
        ["severity", "lifecycle", "triage", "tool", "ruleId", "filePath", "line", "firstSeenAt", "lastSeenAt"];

    /// <summary>Parses <paramref name="input"/>. Returns <c>null</c> and fills <paramref name="errors"/> when invalid.</summary>
    public static FindingsQuery? Parse(FindingsQueryInput input, out IReadOnlyDictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(input);
        var problems = new Dictionary<string, string[]>(StringComparer.Ordinal);

        var page = input.Page ?? 0;
        if (page is < 0 or > MaxPage)
        {
            problems["page"] = [$"Page must be between 0 and {MaxPage}."];
        }

        var pageSize = input.PageSize ?? DefaultPageSize;
        if (!PageSizes.Contains(pageSize))
        {
            problems["pageSize"] = [$"Page size must be one of {string.Join(", ", PageSizes)}."];
        }

        var sort = FindingSortField.Severity;
        if (!string.IsNullOrWhiteSpace(input.Sort) && !SortFields.TryGetValue(input.Sort.Trim(), out sort))
        {
            problems["sort"] = [$"Sort must be one of: {string.Join(", ", SortFieldNames)}."];
        }

        var direction = SortDirection.Desc;
        if (!string.IsNullOrWhiteSpace(input.Dir))
        {
            direction = input.Dir.Trim().ToUpperInvariant() switch
            {
                "ASC" => SortDirection.Asc,
                "DESC" => SortDirection.Desc,
                _ => Invalid(problems, "dir", "Direction must be 'asc' or 'desc'.", SortDirection.Desc),
            };
        }

        var severities = ParseEnumList<Severity>(input.Severity, "severity", problems);
        var statuses = ParseEnumList<Lifecycle>(input.Status, "status", problems);
        var triage = ParseEnumList<TriageStatus>(input.Triage, "triage", problems);

        var tools = SplitList(input.Tool);
        if (tools.Count > MaxTools || tools.Any(t => t.Length > MaxToolNameLength))
        {
            problems["tool"] = [$"Give at most {MaxTools} tool names of up to {MaxToolNameLength} characters."];
        }

        var search = string.IsNullOrWhiteSpace(input.Q) ? null : input.Q.Trim();
        if (search?.Length > MaxSearchLength)
        {
            problems["q"] = [$"Search text is at most {MaxSearchLength} characters."];
        }

        if (input.Scan is < 1)
        {
            problems["scan"] = ["Scan numbers start at 1."];
        }

        errors = problems;
        return problems.Count > 0
            ? null
            : new FindingsQuery(page, pageSize, sort, direction, severities, statuses, triage, tools, search, input.Scan);
    }

    private static List<string> SplitList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.Ordinal)];

    private static List<TEnum> ParseEnumList<TEnum>(string? value, string parameter, Dictionary<string, string[]> problems)
        where TEnum : struct, Enum
    {
        var result = new List<TEnum>();
        var invalid = new List<string>();
        foreach (var item in SplitList(value))
        {
            // Match names only: Enum.TryParse would also accept "1" or "1,2".
            var name = Array.Find(Enum.GetNames<TEnum>(), n => string.Equals(n, item, StringComparison.OrdinalIgnoreCase));
            if (name is null)
            {
                invalid.Add(item);
            }
            else
            {
                result.Add(Enum.Parse<TEnum>(name));
            }
        }

        if (invalid.Count > 0)
        {
            problems[parameter] = [$"Unknown value(s) '{string.Join("', '", invalid)}'. Allowed: {string.Join(", ", Enum.GetNames<TEnum>())}."];
        }

        return result;
    }

    private static T Invalid<T>(Dictionary<string, string[]> problems, string parameter, string message, T fallback)
    {
        problems[parameter] = [message];
        return fallback;
    }
}
