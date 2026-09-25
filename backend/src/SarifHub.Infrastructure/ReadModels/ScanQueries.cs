using Dapper;
using Npgsql;
using SarifHub.Application.Scans;

namespace SarifHub.Infrastructure.ReadModels;

/// <summary>Scan list and scan details (Dapper over the immutable scan snapshots).</summary>
internal sealed class ScanQueries(NpgsqlDataSource dataSource) : IScanQueries
{
    public async Task<IReadOnlyList<ScanSummaryDto>> ListAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<ScanSummaryRow>(new CommandDefinition(
            ReadSql.ScanSummaries + "\nORDER BY s.number DESC",
            new { projectId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        return [.. rows.Select(r => r.ToSummary())];
    }

    public async Task<ScanDetailDto?> GetAsync(Guid projectId, int number, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<ScanSummaryRow>(new CommandDefinition(
            ReadSql.ScanSummaries + "\nAND s.number = @number",
            new { projectId, number },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row?.ToDetail();
    }
}
