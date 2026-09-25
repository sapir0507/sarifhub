using Dapper;
using Npgsql;
using SarifHub.Application.Trends;

namespace SarifHub.Infrastructure.ReadModels;

/// <summary>Trend points (Dapper; reads one row per scan from the scan snapshots).</summary>
internal sealed class TrendQuery(NpgsqlDataSource dataSource) : ITrendQuery
{
    public async Task<IReadOnlyList<TrendPointDto>> GetAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<TrendPointRow>(new CommandDefinition(
            ReadSql.TrendPoints,
            new { projectId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        return [.. rows.Select(r => r.ToDto())];
    }
}
