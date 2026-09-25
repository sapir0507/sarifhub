namespace SarifHub.Domain.Scans;

/// <summary>Who uploaded a scan: exactly one of a user or a CI API key.</summary>
public readonly record struct ScanUploader
{
    private ScanUploader(Guid? userId, Guid? apiKeyId)
    {
        UserId = userId;
        ApiKeyId = apiKeyId;
    }

    /// <summary>The uploading user, for manual uploads.</summary>
    public Guid? UserId { get; }

    /// <summary>The API key, for CI uploads.</summary>
    public Guid? ApiKeyId { get; }

    /// <summary>A manual upload from the UI.</summary>
    public static ScanUploader User(Guid userId) => new(userId, null);

    /// <summary>An upload from CI.</summary>
    public static ScanUploader ApiKey(Guid apiKeyId) => new(null, apiKeyId);
}
