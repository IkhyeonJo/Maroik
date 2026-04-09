using Maroik.Core.Client.Clients;

namespace Maroik.Core.Client.Tests.Clients;

public class FileClientTests
{
    private readonly FileClient _sut = new();

    // ── UploadAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_ReturnsFalse_WhenFileDataIsNull()
    {
        bool result = await _sut.UploadAsync(null!, "image/png", "test.png", "http://localhost");

        Assert.False(result);
    }

    [Fact]
    public async Task UploadAsync_ReturnsFalse_WhenFileDataIsEmpty()
    {
        bool result = await _sut.UploadAsync([], "image/png", "test.png", "http://localhost");

        Assert.False(result);
    }

    [Fact]
    public async Task UploadAsync_ReturnsFalse_WhenHttpCallFails()
    {
        // Any non-empty byte array will attempt a real HTTP call to an invalid host → returns false (caught exception)
        byte[] data = [0x89, 0x50, 0x4E, 0x47];
        bool result = await _sut.UploadAsync(data, "image/png", "test.png", "http://invalid.host.that.does.not.exist.local");

        Assert.False(result);
    }
}
