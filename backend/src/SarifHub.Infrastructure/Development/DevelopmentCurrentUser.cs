using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SarifHub.Application.Common;
using SarifHub.Domain.Users;
using SarifHub.Infrastructure.Persistence;

namespace SarifHub.Infrastructure.Development;

/// <summary>Configuration of the Development-only acting user (section <c>Development</c>).</summary>
public sealed class DevelopmentOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Development";

    /// <summary>Email of the seeded user every request acts as until authentication exists.</summary>
    public string ActingUserEmail { get; set; } = string.Empty;
}

/// <summary>
/// Development-only <see cref="ICurrentUser"/> (ADR 0011): every request acts as the configured seeded user.
/// There is no login, token or password here, and the API refuses to start outside Development until Phase 6
/// replaces this class with one that reads the authenticated principal.
/// </summary>
public sealed partial class DevelopmentCurrentUser(
    SarifHubDbContext db,
    IOptions<DevelopmentOptions> options,
    ILogger<DevelopmentCurrentUser> logger) : ICurrentUser
{
    private Guid? _userId;
    private bool _resolved;

    /// <inheritdoc />
    public async Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken)
    {
        if (_resolved)
        {
            return _userId;
        }

        var email = options.Value.ActingUserEmail;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalized = User.Normalize(email);
            _userId = await db.Users.AsNoTracking()
                .Where(u => u.NormalizedEmail == normalized)
                .Select(u => (Guid?)u.Id)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        if (_userId is null)
        {
            LogActingUserMissing(logger);
        }

        _resolved = true;
        return _userId;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The Development acting user was not found. Set Development:ActingUserEmail and run the seed (see README).")]
    private static partial void LogActingUserMissing(ILogger logger);
}
