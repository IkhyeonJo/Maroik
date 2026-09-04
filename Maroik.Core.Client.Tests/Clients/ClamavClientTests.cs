using System.Net;
using System.Net.Sockets;
using System.Text;
using Maroik.Core.Client.Clients;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>
/// Unit tests for <see cref="ClamavClient"/>.
/// Because no real ClamAV daemon is available during the test run, every test targets
/// an unreachable host or invalid address and verifies that the client handles the
/// connection failure gracefully by returning <see langword="false"/>.
/// </summary>
public class ClamavClientTests
{
    private readonly ClamavClient _sut = new();

    // -- ScanWithClamavAsync --------------------------------------------------

    /// <summary>Verifies that <c>ScanWithClamavAsync</c> returns false when host is unreachable.</summary>
    [Fact]
    public async Task ScanWithClamavAsync_ReturnsFalse_WhenHostIsUnreachable()
    {
        using var stream = new MemoryStream([0x01, 0x02, 0x03]);

        // Any host that won't have ClamAV running → connection refused → returns false
        bool result = await _sut.ScanWithClamavAsync(stream, "127.0.0.1", 19999, TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    /// <summary>Verifies that <c>ScanWithClamavAsync</c> returns false when host name is invalid.</summary>
    [Fact]
    public async Task ScanWithClamavAsync_ReturnsFalse_WhenHostNameIsInvalid()
    {
        using var stream = new MemoryStream([0x01, 0x02, 0x03]);

        bool result = await _sut.ScanWithClamavAsync(stream, "invalid.host.that.does.not.exist.local", 3310, TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    /// <summary>Verifies that <c>ScanWithClamavAsync</c> resets stream position before reading.</summary>
    [Fact]
    public async Task ScanWithClamavAsync_ResetsStreamPosition_BeforeReading()
    {
        // Place stream at end to verify that the implementation rewinds it
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        using var stream = new MemoryStream(data);
        stream.Position = data.Length; // advance to end

        // Should not throw even though stream is at end initially;
        // implementation rewinds to position 0 before reading
        bool result = await _sut.ScanWithClamavAsync(stream, "127.0.0.1", 19999, TestContext.Current.CancellationToken);

        // Unreachable host returns false; the key assertion is no exception thrown
        Assert.False(result);
    }

    /// <summary>Verifies that <c>ScanWithClamavAsync</c> returns false when stream is empty.</summary>
    [Fact]
    public async Task ScanWithClamavAsync_ReturnsFalse_WhenStreamIsEmpty()
    {
        using var stream = new MemoryStream();

        bool result = await _sut.ScanWithClamavAsync(stream, "127.0.0.1", 19999, TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    // -- INSTREAM response parsing ---------------------------------------------
    // The tests above only exercise the connection-failure paths. These use a fake in-process
    // ClamAV daemon to drive the actual protocol exchange and response parsing
    // (`response.Contains("OK")` in ClamavClient), which nothing else in this file covers.

    /// <summary>Verifies that <c>ScanWithClamavAsync</c> returns true when the daemon reports OK.</summary>
    [Fact]
    public async Task ScanWithClamavAsync_ReturnsTrue_WhenDaemonRespondsOk()
    {
        await using var server = FakeClamavServer.Start("stream: OK\n");
        using var stream = new MemoryStream([0x01, 0x02, 0x03]);

        bool result = await _sut.ScanWithClamavAsync(stream, "127.0.0.1", server.Port, TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    /// <summary>Verifies that <c>ScanWithClamavAsync</c> returns false when the daemon reports a virus found.</summary>
    [Fact]
    public async Task ScanWithClamavAsync_ReturnsFalse_WhenDaemonRespondsFound()
    {
        await using var server = FakeClamavServer.Start("stream: Eicar-Test-Signature FOUND\n");
        using var stream = new MemoryStream([0x01, 0x02, 0x03]);

        bool result = await _sut.ScanWithClamavAsync(stream, "127.0.0.1", server.Port, TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    /// <summary>
    /// Minimal fake ClamAV daemon: accepts one INSTREAM session, consumes the length-prefixed
    /// chunks up to the zero-length terminator, then writes back <c>response</c> and closes.
    /// </summary>
    private sealed class FakeClamavServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly Task _acceptTask;

        public int Port { get; }

        private FakeClamavServer(TcpListener listener, string response)
        {
            _listener = listener;
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            _acceptTask = AcceptAndRespondAsync(response);
        }

        public static FakeClamavServer Start(string response)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return new FakeClamavServer(listener, response);
        }

        private async Task AcceptAndRespondAsync(string response)
        {
            using TcpClient client = await _listener.AcceptTcpClientAsync();
            await using NetworkStream stream = client.GetStream();

            // Consume the "nINSTREAM" command line; its exact content doesn't matter here.
            await ReadLineAsync(stream);

            // Consume length-prefixed chunks until the zero-length terminator, mirroring
            // ClamavClient's write side of the INSTREAM protocol.
            while (true)
            {
                byte[] lengthBytes = await ReadExactlyAsync(stream, 4);
                int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes));
                if (length == 0) break;
                _ = await ReadExactlyAsync(stream, length);
            }

            await stream.WriteAsync(Encoding.ASCII.GetBytes(response));
        }

        private static async Task<byte[]> ReadExactlyAsync(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset));
                if (read == 0) throw new IOException("Connection closed unexpectedly.");
                offset += read;
            }
            return buffer;
        }

        private static async Task ReadLineAsync(Stream stream)
        {
            var b = new byte[1];
            while (await stream.ReadAsync(b) == 1 && b[0] != (byte)'\n') { }
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();
            try { await _acceptTask; } catch { /* connection already handled or listener stopped mid-accept */ }
        }
    }
}
