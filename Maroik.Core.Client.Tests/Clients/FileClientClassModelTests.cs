using System.Net;
using Maroik.Core.Client.Clients;
using Maroik.Core.Contract.Messages;
using Maroik.Core.Contract.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>
/// Unit tests for the class-model calls of <see cref="FileClient"/>: a save sends the model file and its path to
/// <c>api/ClassModel/save</c>, a delete sends the path to <c>api/ClassModel/delete</c>; each reads the answer back as true / false and
/// logs a refusal (Warning) or a transport failure (Error) with its target — never throws to the caller.
/// </summary>
public class FileClientClassModelTests
{
    /// <summary>The base URL of the fake storage service.</summary>
    private const string BaseUrl = "http://files.local";

    /// <summary>The model path these tests use.</summary>
    private const string ModelPath = "upload/ClassModels/0f8fad5b-d9cb-469f-a165-70867728950e.zip";

    /// <summary>Answers every request with a fixed status and body, recording what was sent.</summary>
    private sealed class FakeStorage(HttpStatusCode status, string body) : HttpMessageHandler
    {
        /// <summary>The URL of the last request.</summary>
        public string? Url { get; private set; }

        /// <summary>The body of the last request, read when it was sent.</summary>
        public byte[]? Body { get; private set; }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri?.ToString();
            Body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
            HttpResponseMessage? response = new(status);
            try
            {
                response.Content = new StringContent(body);
                HttpResponseMessage built = response;
                response = null;
                return built;
            }
            finally
            {
                response?.Dispose();
            }
        }
    }

    /// <summary>Fails every request as an unreachable storage service would.</summary>
    private sealed class Unreachable : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("connection refused");
    }

    /// <summary>Hands out clients over one handler (which the clients do not own).</summary>
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        /// <inheritdoc />
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    /// <summary>The captured log.</summary>
    private readonly FakeLogger<FileClient> _logger = new();

    /// <summary>A client over <paramref name="handler"/>.</summary>
    private FileClient Sut(HttpMessageHandler handler) => new(new Factory(handler), Options.Create(new ServerSetting { FileStorageBaseUrl = BaseUrl }), _logger);

    /// <summary>The bytes of <paramref name="text"/> are somewhere in <paramref name="body"/>.</summary>
    private static bool Contains(byte[]? body, byte[] text) => body != null && body.AsSpan().IndexOf(text) >= 0;

    /// <summary>A save posts the path and the model's bytes to api/ClassModel/save and is true when stored.</summary>
    [Fact]
    public async Task SaveClassModelAsync_SendsThePathAndTheFile()
    {
        using var storage = new FakeStorage(HttpStatusCode.OK, "");
        byte[] model = [0x50, 0x4B, 0x03, 0x04, 0x2A, 0x2B];

        Assert.True(await Sut(storage).SaveClassModelAsync(ModelPath, model, TestContext.Current.CancellationToken));

        Assert.Equal($"{BaseUrl}/api/ClassModel/save", storage.Url);
        Assert.True(Contains(storage.Body, System.Text.Encoding.UTF8.GetBytes(ModelPath)));
        Assert.True(Contains(storage.Body, model));
    }

    /// <summary>A delete posts the path to api/ClassModel/delete and is true when deleted.</summary>
    [Fact]
    public async Task DeleteClassModelAsync_SendsThePath()
    {
        using var storage = new FakeStorage(HttpStatusCode.OK, "");

        Assert.True(await Sut(storage).DeleteClassModelAsync(ModelPath, TestContext.Current.CancellationToken));

        Assert.Equal($"{BaseUrl}/api/ClassModel/delete", storage.Url);
        Assert.True(Contains(storage.Body, System.Text.Encoding.UTF8.GetBytes(ModelPath)));
    }

    /// <summary>A refused save or delete is false, logged at Warning with the path, status and reason.</summary>
    [Fact]
    public async Task ARefusal_IsFalse_AndLoggedAtWarning()
    {
        using var storage = new FakeStorage(HttpStatusCode.Conflict, FileStorageRefusals.FileExists);

        Assert.False(await Sut(storage).SaveClassModelAsync(ModelPath, [1], TestContext.Current.CancellationToken));
        Assert.False(await Sut(storage).DeleteClassModelAsync(ModelPath, TestContext.Current.CancellationToken));

        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Warning, Message: $"Class model save of {ModelPath} returned 409: {FileStorageRefusals.FileExists}" });
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Warning, Message: $"Class model delete of {ModelPath} returned 409: {FileStorageRefusals.FileExists}" });
    }

    /// <summary>An unreachable storage service is false, logged at Error with the exception and the path.</summary>
    [Fact]
    public async Task AnUnreachableService_IsFalse_AndLoggedAtError()
    {
        using var handler = new Unreachable();

        Assert.False(await Sut(handler).SaveClassModelAsync(ModelPath, [1], TestContext.Current.CancellationToken));
        Assert.False(await Sut(handler).DeleteClassModelAsync(ModelPath, TestContext.Current.CancellationToken));

        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Error, Exception: HttpRequestException, Message: $"Class model save request failed for {ModelPath}" });
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Error, Exception: HttpRequestException, Message: $"Class model delete request failed for {ModelPath}" });
    }
}
