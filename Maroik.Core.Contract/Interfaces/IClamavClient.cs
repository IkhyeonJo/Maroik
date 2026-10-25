using Maroik.Core.Contract.Enums;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Client interface for scanning file streams with a ClamAV antivirus daemon.
/// The daemon address is configured in <see cref="Maroik.Core.Contract.Settings.ServerSetting"/>.
/// </summary>
public interface IClamavClient
{
    /// <summary>
    /// Streams <paramref name="fileStream"/> to the ClamAV daemon via the INSTREAM protocol.
    /// Returns <see cref="ClamavScanResult.Clean"/> when the daemon reports the file clean,
    /// <see cref="ClamavScanResult.Infected"/> on a detection, <see cref="ClamavScanResult.LimitExceeded"/> when the stream is longer
    /// than ClamAV can scan or clamd hit one of its limits, and <see cref="ClamavScanResult.Unavailable"/>
    /// when the scan could not be completed (connection error, timeout, daemon error, unreadable reply).
    /// Anything other than <see cref="ClamavScanResult.Clean"/> must be treated as a refusal.
    /// </summary>
    public Task<ClamavScanResult> ScanWithClamavAsync(Stream fileStream, string clamavHost, int clamavPort, CancellationToken ct = default);

    /// <summary>
    /// The same scan as <see cref="ScanWithClamavAsync(Stream, string, int, CancellationToken)"/>, bounded by
    /// <paramref name="scanTimeout"/> instead of the default time limit — for a caller that scans files far larger than an
    /// attachment (a File Manager file of up to 2 GB takes minutes).
    /// </summary>
    public Task<ClamavScanResult> ScanWithClamavAsync(Stream fileStream, string clamavHost, int clamavPort, TimeSpan scanTimeout, CancellationToken ct = default);
}
