using System.Diagnostics;
using System.Net;
using Maroik.Core.Client.Clients;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>
/// Unit tests for <see cref="FileClient"/>.
/// Exercises null/empty input guards, connection-failure handling when the file-storage
/// microservice is not available, and correlation-id propagation.
/// </summary>
public class FileClientTests
{
    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpMessageHandler? InnerHandler { get; set; }
        public HttpClient CreateClient(string name) => InnerHandler is null ? new HttpClient() : new HttpClient(InnerHandler);
    }

    /// <summary>
    /// Reads the request body into a string immediately, since <see cref="FileClient"/>
    /// disposes its <see cref="MultipartFormDataContent"/> in a <c>using</c> block right after
    /// the call returns — inspecting <c>request.Content</c> after the fact would throw
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private readonly FakeHttpClientFactory _httpClientFactory = new();
    private FileClient CreateSut() => new(_httpClientFactory, NullLogger<FileClient>.Instance);

    // -- UploadAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>UploadAsync</c> returns false when file data is null.</summary>
    [Fact]
    public async Task UploadAsync_ReturnsFalse_WhenFileDataIsNull()
    {
        bool result = await CreateSut().UploadAsync(null!, "image/png", "test.png", "http://localhost", TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    /// <summary>Verifies that <c>UploadAsync</c> returns false when file data is empty.</summary>
    [Fact]
    public async Task UploadAsync_ReturnsFalse_WhenFileDataIsEmpty()
    {
        bool result = await CreateSut().UploadAsync([], "image/png", "test.png", "http://localhost", TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    /// <summary>Verifies that <c>UploadAsync</c> returns false when http call fails.</summary>
    [Fact]
    public async Task UploadAsync_ReturnsFalse_WhenHttpCallFails()
    {
        // Any non-empty byte array will attempt a real HTTP call to an invalid host → returns false (caught exception)
        byte[] data = [0x89, 0x50, 0x4E, 0x47];
        bool result = await CreateSut().UploadAsync(data, "image/png", "test.png", "http://invalid.host.that.does.not.exist.local", TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    // -- Correlation id form field ----------------------------------------------

    /// <summary>Verifies that <c>UploadAsync</c> includes the ambient Activity id as a form field.</summary>
    [Fact]
    public async Task UploadAsync_IncludesCorrelationId_WhenActivityIsCurrent()
    {
        using var activity = new Activity("test-request").Start();
        var capturing = new CapturingHandler();
        _httpClientFactory.InnerHandler = capturing;
        byte[] data = [0x89, 0x50, 0x4E, 0x47];

        await CreateSut().UploadAsync(data, "image/png", "test.png", "http://filestorage.local", TestContext.Current.CancellationToken);

        Assert.Contains("name=correlationId", capturing.LastRequestBody);
        Assert.Contains(activity.Id!, capturing.LastRequestBody);
    }

    /// <summary>Verifies that <c>DownloadAsync</c> includes the ambient Activity id as a form field.</summary>
    [Fact]
    public async Task DownloadAsync_IncludesCorrelationId_WhenActivityIsCurrent()
    {
        using var activity = new Activity("test-request").Start();
        var capturing = new CapturingHandler();
        _httpClientFactory.InnerHandler = capturing;

        await CreateSut().DownloadAsync("some/path.png", "http://filestorage.local", TestContext.Current.CancellationToken);

        Assert.Contains("name=correlationId", capturing.LastRequestBody);
        Assert.Contains(activity.Id!, capturing.LastRequestBody);
    }

    /// <summary>Verifies that <c>DownloadAsync</c> sends an empty correlation id when there is no ambient Activity.</summary>
    [Fact]
    public async Task DownloadAsync_SendsEmptyCorrelationId_WhenNoActivityIsCurrent()
    {
        Activity.Current = null;
        var capturing = new CapturingHandler();
        _httpClientFactory.InnerHandler = capturing;

        await CreateSut().DownloadAsync("some/path.png", "http://filestorage.local", TestContext.Current.CancellationToken);

        Assert.Contains("name=correlationId", capturing.LastRequestBody);
    }
}
