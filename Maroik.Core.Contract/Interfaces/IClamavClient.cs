namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Client interface for scanning file streams with a ClamAV antivirus daemon.
/// The daemon address is configured in <see cref="Maroik.Core.Contract.Misc.Settings.ServerSetting"/>.
/// </summary>
public interface IClamavClient
{
    /// <summary>
    /// Streams <paramref name="fileStream"/> to the ClamAV daemon via the INSTREAM protocol.
    /// Returns true when the file is clean; false on a virus detection or connection error.
    /// </summary>
    public Task<bool> ScanWithClamavAsync(Stream fileStream, string clamavHost, int clamavPort, CancellationToken ct = default);
}
