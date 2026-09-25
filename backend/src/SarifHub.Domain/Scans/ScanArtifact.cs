namespace SarifHub.Domain.Scans;

/// <summary>
/// The raw uploaded SARIF file, gzip-compressed. Kept so findings can be re-derived when the parser or the
/// fingerprint algorithm changes. Written by ingestion (Phase 5).
/// </summary>
public sealed class ScanArtifact
{
    private byte[] _contentGzip = [];

    private ScanArtifact()
    {
    }

    /// <summary>The scan (also the primary key).</summary>
    public Guid ScanId { get; private set; }

    /// <summary>Compressed content.</summary>
    public IReadOnlyList<byte> ContentGzip => _contentGzip;

    /// <summary>Size of the uncompressed upload.</summary>
    public long OriginalBytes { get; private set; }

    /// <summary>Creates the artifact for a scan.</summary>
    public static ScanArtifact Create(Guid scanId, IReadOnlyList<byte> contentGzip, long originalBytes)
    {
        ArgumentNullException.ThrowIfNull(contentGzip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(originalBytes);
        return new ScanArtifact { ScanId = scanId, _contentGzip = [.. contentGzip], OriginalBytes = originalBytes };
    }
}
