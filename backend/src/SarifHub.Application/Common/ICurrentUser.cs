namespace SarifHub.Application.Common;

/// <summary>
/// The caller of the current request. Until authentication exists (Phase 6) the only implementation is the
/// Development-only acting user (ADR 0011); afterwards it reads the authenticated principal.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The calling user's id, or <c>null</c> when the caller is not a known user.</summary>
    Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken);
}
