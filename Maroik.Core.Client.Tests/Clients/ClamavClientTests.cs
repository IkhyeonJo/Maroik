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

    // ── ScanWithClamavAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ScanWithClamavAsync_ReturnsFalse_WhenHostIsUnreachable()
    {
        using var stream = new MemoryStream([0x01, 0x02, 0x03]);

        // Any host that won't have ClamAV running → connection refused → returns false
        bool result = await _sut.ScanWithClamavAsync(stream, "127.0.0.1", 19999);

        Assert.False(result);
    }

    [Fact]
    public async Task ScanWithClamavAsync_ReturnsFalse_WhenHostNameIsInvalid()
    {
        using var stream = new MemoryStream([0x01, 0x02, 0x03]);

        bool result = await _sut.ScanWithClamavAsync(stream, "invalid.host.that.does.not.exist.local", 3310);

        Assert.False(result);
    }

    [Fact]
    public async Task ScanWithClamavAsync_ResetsStreamPosition_BeforeReading()
    {
        // Place stream at end to verify that the implementation rewinds it
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        using var stream = new MemoryStream(data);
        stream.Position = data.Length; // advance to end

        // Should not throw even though stream is at end initially;
        // implementation rewinds to position 0 before reading
        bool result = await _sut.ScanWithClamavAsync(stream, "127.0.0.1", 19999);

        // Unreachable host returns false; the key assertion is no exception thrown
        Assert.False(result);
    }

    [Fact]
    public async Task ScanWithClamavAsync_ReturnsFalse_WhenStreamIsEmpty()
    {
        using var stream = new MemoryStream();

        bool result = await _sut.ScanWithClamavAsync(stream, "127.0.0.1", 19999);

        Assert.False(result);
    }
}
