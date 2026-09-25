using SarifHub.Application.Common;

namespace SarifHub.Application.Tools;

/// <summary>Read model for the tools that reported findings in a project (filter options).</summary>
public interface IToolQuery
{
    /// <summary>Distinct tool names, sorted.</summary>
    Task<IReadOnlyList<string>> ListAsync(Guid projectId, CancellationToken cancellationToken);
}

/// <summary>Use case: tool names for the findings filter. <c>null</c> when the project is not visible to the caller.</summary>
public sealed class ListTools(ProjectReadAccess access, IToolQuery query)
{
    /// <summary>Runs the use case.</summary>
    public async Task<IReadOnlyList<string>?> ExecuteAsync(Guid projectId, CancellationToken cancellationToken) =>
        await access.CanReadAsync(projectId, cancellationToken).ConfigureAwait(false)
            ? await query.ListAsync(projectId, cancellationToken).ConfigureAwait(false)
            : null;
}
