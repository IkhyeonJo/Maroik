using Maroik.Core.Client.Clients;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>
/// Unit tests for <see cref="FileClient"/>.
/// Exercises null/empty input guards and connection-failure handling when the
/// file-storage microservice is not available.
/// </summary>
public class FileClientTests
{
    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private readonly FileClient _sut = new(new FakeHttpClientFactory());

    // -- UploadAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>UploadAsync</c> returns false when file data is null.</summary>
    [Fact]
    public async Task UploadAsync_ReturnsFalse_WhenFileDataIsNull()
    {
        bool result = await _sut.UploadAsync(null!, "image/png", "test.png", "http://localhost", TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    /// <summary>Verifies that <c>UploadAsync</c> returns false when file data is empty.</summary>
    [Fact]
    public async Task UploadAsync_ReturnsFalse_WhenFileDataIsEmpty()
    {
        bool result = await _sut.UploadAsync([], "image/png", "test.png", "http://localhost", TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    /// <summary>Verifies that <c>UploadAsync</c> returns false when http call fails.</summary>
    [Fact]
    public async Task UploadAsync_ReturnsFalse_WhenHttpCallFails()
    {
        // Any non-empty byte array will attempt a real HTTP call to an invalid host → returns false (caught exception)
        byte[] data = [0x89, 0x50, 0x4E, 0x47];
        bool result = await _sut.UploadAsync(data, "image/png", "test.png", "http://invalid.host.that.does.not.exist.local", TestContext.Current.CancellationToken);

        Assert.False(result);
    }
}
