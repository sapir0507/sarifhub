using Microsoft.EntityFrameworkCore;
using SarifHub.Application.Findings;
using SarifHub.Infrastructure.Persistence;

namespace SarifHub.Infrastructure.ReadModels;

/// <summary>
/// One finding with its history (EF Core, no tracking): a read by id, which ADR 0003 leaves to EF Core.
/// Three small queries — finding with rule, occurrences, triage history — each served by an index.
/// </summary>
internal sealed class FindingDetailQuery(SarifHubDbContext db) : IFindingDetailQuery
{
    /// <summary>Shown as the decider of system decisions (for example, a lapsed acceptance).</summary>
    private const string SystemDecider = "SarifHub";

    public async Task<VersionedFindingDetail?> GetAsync(Guid projectId, Guid findingId, CancellationToken cancellationToken)
    {
        var header = await (
                from f in db.Findings.AsNoTracking()
                join r in db.Rules on f.RuleId equals r.Id
                join first in db.Scans on f.FirstSeenScanId equals first.Id
                join last in db.Scans on f.LastSeenScanId equals last.Id
                where f.Id == findingId && f.ProjectId == projectId
                select new { Finding = f, Rule = r, FirstScan = first.Number, LastScan = last.Number })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (header is null)
        {
            return null;
        }

        var occurrences = await (
                from o in db.FindingOccurrences.AsNoTracking()
                join s in db.Scans on o.ScanId equals s.Id
                where o.FindingId == findingId
                orderby s.Number
                select new { o.ScanId, s.Number, s.UploadedAt, o.FilePath, o.StartLine, o.Change, o.Snippet })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var decisions = await (
                from d in db.TriageDecisions.AsNoTracking()
                join u in db.Users on d.DecidedById equals u.Id into deciders
                from u in deciders.DefaultIfEmpty()
                where d.FindingId == findingId
                orderby d.DecidedAt
                select new { d.Id, d.Status, d.Reason, d.ExpiresAt, DecidedBy = u == null ? null : u.DisplayName, d.DecidedAt })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var finding = header.Finding;
        var rule = header.Rule;
        var latest = occurrences.LastOrDefault(o => o.Snippet is not null);
        var snippetLine = latest?.StartLine ?? finding.StartLine ?? 1;
        var snippet = new CodeSnippetDto(
            snippetLine,
            snippetLine,
            latest?.Snippet is { } text ? text.Split('\n') : []);

        var detail = new FindingDetailDto(
            finding.Id,
            finding.Severity,
            finding.Lifecycle,
            finding.TriageStatus,
            rule.ToolName,
            rule.RuleId,
            rule.Name ?? rule.RuleId,
            finding.FilePath,
            finding.StartLine,
            finding.FirstSeenAt,
            finding.LastSeenAt,
            header.FirstScan,
            header.LastScan,
            finding.AcceptedRiskExpiresAt,
            finding.Message,
            rule.ShortDescription ?? string.Empty,
            rule.Cwe,
            finding.Fingerprint,
            snippet,
            [.. occurrences.Select(o => new FindingOccurrenceDto(o.ScanId, o.Number, o.UploadedAt, o.FilePath, o.StartLine, o.Change))],
            [.. decisions.Select(d => new TriageDecisionDto(d.Id, d.Status, d.Reason, d.ExpiresAt, d.DecidedBy ?? SystemDecider, d.DecidedAt))]);

        return new VersionedFindingDetail(detail, finding.Version);
    }
}
