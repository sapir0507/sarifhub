using Microsoft.EntityFrameworkCore;
using SarifHub.Application.Tools;
using SarifHub.Infrastructure.Persistence;

namespace SarifHub.Infrastructure.ReadModels;

/// <summary>Distinct tools with findings in a project (EF Core: a simple read, no report shaping).</summary>
internal sealed class ToolQuery(SarifHubDbContext db) : IToolQuery
{
    public async Task<IReadOnlyList<string>> ListAsync(Guid projectId, CancellationToken cancellationToken) =>
        await (
                from f in db.Findings.AsNoTracking()
                join r in db.Rules on f.RuleId equals r.Id
                where f.ProjectId == projectId
                select r.ToolName)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
