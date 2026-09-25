using Dapper;
using Npgsql;
using SarifHub.Application.Projects;

namespace SarifHub.Infrastructure.ReadModels;

/// <summary>Projects list (Dapper: one query with a lateral count per project).</summary>
internal sealed class ProjectQueries(NpgsqlDataSource dataSource) : IProjectQueries
{
    public async Task<IReadOnlyList<ProjectSummaryDto>> ListForMemberAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<ProjectSummaryRow>(new CommandDefinition(
            ReadSql.ProjectSummaries + "\nORDER BY p.name",
            new { userId, now },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        return [.. rows.Select(r => r.ToDto())];
    }
}
