namespace Maroik.Core.Contract.Dtos;

/// <summary>The upload timings the File Manager page follows; authoritative in the Domain.</summary>
public sealed class FileManagerFormRules
{
    /// <summary>How often the page asks about a running scan.</summary>
    public int ScanPollMilliseconds { get; init; }

    /// <summary>How long the page waits for a scan before giving up.</summary>
    public int MaxScanWaitMilliseconds { get; init; }
}
