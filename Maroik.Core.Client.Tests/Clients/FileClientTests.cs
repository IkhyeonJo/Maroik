using System.Diagnostics;
using System.Net;
using Maroik.Core.Client.Clients;
using Microsoft.Extensions.Logging;
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
        /// <summary>The handler the created <see cref="HttpClient"/> should use, or null for a default client.</summary>
        public HttpMessageHandler? InnerHandler { get; set; }

        /// <summary>Creates an <see cref="HttpClient"/> wrapping <see cref="InnerHandler"/> when one is set.</summary>
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
        /// <summary>The body of the most recently captured request, or null if it had no content.</summary>
        public string? LastRequestBody { get; private set; }

        /// <summary>Captures the request body and returns a canned 200 OK response.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    /// <summary>Returns a fixed status + plain-text body, like Maroik.FileStorage's <c>BadRequest(error)</c>.</summary>
    private sealed class RefusingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    /// <summary>Records every formatted log message.</summary>
    private sealed class RecordingLogger : ILogger<FileClient>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
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

    // -- Refusal reason logging -------------------------------------------------

    /// <summary>
    /// Verifies that when the storage service refuses an upload, the reason it gave in the response
    /// body ("could not be scanned", "infected", ...) is logged — a bare "400" would not tell an
    /// operator a ClamAV outage from a virus detection.
    /// </summary>
    [Fact]
    public async Task UploadAsync_LogsTheStorageServicesRefusalReason_WhenUploadIsRefused()
    {
        _httpClientFactory.InnerHandler = new RefusingHandler(HttpStatusCode.BadRequest, "The file could not be scanned for viruses. Please try again later.");
        var logger = new RecordingLogger();
        var sut = new FileClient(_httpClientFactory, logger);

        bool result = await sut.UploadAsync([0x89, 0x50, 0x4E, 0x47], "image/png", "test.png", "http://filestorage.local", TestContext.Current.CancellationToken);

        Assert.False(result);
        var warning = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("400", warning.Message);
        Assert.Contains("could not be scanned", warning.Message);
    }

    /// <summary>An over-long refusal body is truncated in the log.</summary>
    [Fact]
    public async Task UploadAsync_TruncatesAnOverlongRefusalReason()
    {
        _httpClientFactory.InnerHandler = new RefusingHandler(HttpStatusCode.BadRequest, new string('x', 5000));
        var logger = new RecordingLogger();
        var sut = new FileClient(_httpClientFactory, logger);

        await sut.UploadAsync([0x89, 0x50, 0x4E, 0x47], "image/png", "test.png", "http://filestorage.local", TestContext.Current.CancellationToken);

        var warning = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.True(warning.Message.Length < 500);
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

    // -- Path safety ------------------------------------------------------------

    /// <summary>
    /// Regression test: a leading backslash is not "rooted" per <see cref="Path.IsPathRooted(string)"/>
    /// on this (Linux) runtime, but becomes a rooted Unix path ("/etc/passwd") once normalized for
    /// the storage service — the safety check must normalize separators first, or this form of
    /// input slips past it into an absolute path outside the storage root.
    /// </summary>
    [Theory]
    [InlineData(@"\etc\passwd")]
    [InlineData("/etc/passwd")]
    [InlineData(@"..\..\etc\passwd")]
    [InlineData("../../etc/passwd")]
    public async Task DownloadAsync_ThrowsArgumentException_ForUnsafePath(string unsafePath)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateSut().DownloadAsync(unsafePath, "http://filestorage.local", TestContext.Current.CancellationToken));
    }

    /// <summary>Same unsafe-path cases as <c>DownloadAsync</c>, but <c>UploadAsync</c> reports failure via its bool return instead of throwing.</summary>
    [Theory]
    [InlineData(@"\etc\passwd")]
    [InlineData("/etc/passwd")]
    [InlineData(@"..\..\etc\passwd")]
    [InlineData("../../etc/passwd")]
    public async Task UploadAsync_ReturnsFalse_ForUnsafePath(string unsafePath)
    {
        byte[] data = [0x89, 0x50, 0x4E, 0x47];

        bool result = await CreateSut().UploadAsync(data, "image/png", unsafePath, "http://filestorage.local", TestContext.Current.CancellationToken);

        Assert.False(result);
    }
}
