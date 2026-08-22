using System.Net;
using System.Net.Sockets;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Concrete implementation of <see cref="IClamavClient"/> that streams file data to a
/// ClamAV antivirus daemon using the INSTREAM protocol over a raw TCP connection.
/// </summary>
public class ClamavClient : IClamavClient
{
    // Bounds the whole scan (connect + stream + result) so a stalled or overloaded ClamAV daemon
    // can't pin the calling request thread forever — callers such as FileValidationService don't
    // thread their own CancellationToken into this call, so this is the only backstop.
    private static readonly TimeSpan _scanTimeout = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public async Task<bool> ScanWithClamavAsync(Stream fileStream, string clamavHost, int clamavPort, CancellationToken ct = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_scanTimeout);
        CancellationToken scanCt = timeoutCts.Token;

        try
        {
            // Rewind the stream so we read from the beginning
            fileStream.Position = 0;

            // Open a TCP connection to the ClamAV daemon. ConnectAsync (rather than the
            // TcpClient(host, port) constructor, which blocks the calling thread for the OS
            // connect timeout) honors scanCt so a network-unreachable host can't pin a thread-pool
            // thread for tens of seconds under concurrent uploads.
            using var client = new TcpClient();
            await client.ConnectAsync(clamavHost, clamavPort, scanCt);
            await using var stream = client.GetStream();
            await using var writer = new StreamWriter(stream, leaveOpen: true);
            writer.AutoFlush = true;

            // Initiate a streaming scan session with the INSTREAM command
            await writer.WriteLineAsync("nINSTREAM");

            // Send file data in 2 KB chunks, each prefixed with its 4-byte big-endian length
            byte[] buffer = new byte[2048];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, scanCt)) > 0)
            {
                // INSTREAM protocol requires network byte order (big-endian) for the length prefix
                byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytesRead));
                await stream.WriteAsync(lengthPrefix, scanCt);
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead), scanCt);
            }

            // A zero-length chunk signals end-of-stream to ClamAV
            byte[] zero = BitConverter.GetBytes(0);
            await stream.WriteAsync(zero, scanCt);

            // Read the single-line scan result (e.g. "stream: OK" or "stream: Virus FOUND")
            using var reader = new StreamReader(stream);
            string response = await reader.ReadLineAsync(scanCt) ?? "";

            // "OK" in the response means the file passed the virus scan
            return response.Contains("OK");
        }
        catch
        {
            // Treat any connection, protocol, or timeout error as a scan failure (fail-safe)
            return false;
        }
    }
}
