using System.Net;
using Maroik.Core.Contract.Messages;
using Maroik.FileStorage.Controllers;
using Maroik.FileStorage.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Maroik.FileStorage.Tests.Controllers;

/// <summary>
/// Integration tests for <see cref="ClassModelController"/> (<c>api/ClassModel/*</c>) through the real ASP.NET pipeline, with storage
/// in a throwaway root: save stores a model file (no virus scan), delete removes one, every other path is refused, and each outcome is
/// logged at the level it deserves with the path and the caller's correlation id.
/// </summary>
[Collection("FileStorage host (Program builds a process-wide Serilog logger)")]
public sealed class ClassModelControllerTests(WebApplicationFactory<Program> baseFactory) : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    /// <summary>The throwaway parent of the storage root.</summary>
    private readonly string _base = Path.Combine(Path.GetTempPath(), "cmc-" + Guid.NewGuid().ToString("N"));

    /// <summary>The model path these tests use.</summary>
    private const string ModelPath = "upload/ClassModels/0f8fad5b-d9cb-469f-a165-70867728950e.zip";

    /// <summary>The controller's captured log.</summary>
    private readonly FakeLogger<ClassModelController> _logger = new();

    /// <summary>Removes the throwaway storage.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_base)) Directory.Delete(_base, recursive: true);
    }

    /// <summary>A client for a host storing under the throwaway root.</summary>
    private HttpClient Client() => baseFactory.WithWebHostBuilder(b =>
    {
        b.UseSetting("ServerSetting:StorageRootPath", Path.Combine(_base, "upload"));
        b.ConfigureServices(services => services.AddSingleton<ILogger<ClassModelController>>(_logger));
    }).CreateClient();

    /// <summary>POSTs a save of <paramref name="bytes"/> to <paramref name="path"/>.</summary>
    private static async Task<HttpResponseMessage> SaveAsync(HttpClient client, string path, byte[] bytes)
    {
        using var form = MultipartForm.Of(("filePath", path), ("correlationId", "corr-1"));
        MultipartForm.AddFile(form, "file", "model.zip", bytes, "application/zip");
        return await client.PostAsync("/api/ClassModel/save", form, TestContext.Current.CancellationToken);
    }

    /// <summary>POSTs a delete of <paramref name="path"/>.</summary>
    private static async Task<HttpResponseMessage> DeleteAsync(HttpClient client, string path)
    {
        using var form = MultipartForm.Of(("filePath", path), ("correlationId", "corr-1"));
        return await client.PostAsync("/api/ClassModel/delete", form, TestContext.Current.CancellationToken);
    }

    /// <summary>Save stores the model at its path (Information); a second save of the same path is a conflict (Warning).</summary>
    [Fact]
    public async Task Save_StoresTheModel_AndNeverOverwrites()
    {
        using HttpClient client = Client();

        using var saved = await SaveAsync(client, ModelPath, [1, 2]);
        using var again = await SaveAsync(client, ModelPath, [3]);

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal([1, 2], await File.ReadAllBytesAsync(Path.Combine(_base, "upload", "ClassModels", "0f8fad5b-d9cb-469f-a165-70867728950e.zip"), TestContext.Current.CancellationToken));
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Information, Message: $"Class model saved: {ModelPath} (2 bytes). CorrelationId=corr-1" });
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Warning, Message: $"Class model save refused: {FileStorageRefusals.FileExists}. FilePath={ModelPath} CorrelationId=corr-1" });
    }

    /// <summary>A save outside the model folder is a bad request, logged at Warning.</summary>
    [Fact]
    public async Task Save_RefusesAnyOtherPath()
    {
        using HttpClient client = Client();

        using var refused = await SaveAsync(client, "upload/Forum/a.zip", [1]);

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.False(File.Exists(Path.Combine(_base, "upload", "Forum", "a.zip")));
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Warning, Message: $"Class model save refused: {FileStorageRefusals.NotAClassModelFile}. FilePath=upload/Forum/a.zip CorrelationId=corr-1" });
    }

    /// <summary>A save without a file is a bad request, logged at Warning.</summary>
    [Fact]
    public async Task Save_WithoutAFile_IsRefused()
    {
        using HttpClient client = Client();
        using var form = MultipartForm.Of(("filePath", ModelPath), ("correlationId", "corr-1"));

        using var refused = await client.PostAsync("/api/ClassModel/save", form, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Warning, Message: $"Class model save refused: {FileStorageRefusals.InvalidUpload}. FilePath={ModelPath} CorrelationId=corr-1" });
    }

    /// <summary>Delete removes a model file (Information); a missing one is 404; any other path is refused at Warning.</summary>
    [Fact]
    public async Task Delete_RemovesOnlyAModelFile()
    {
        using HttpClient client = Client();
        using var saved = await SaveAsync(client, ModelPath, [1]);

        using var deleted = await DeleteAsync(client, ModelPath);
        using var missing = await DeleteAsync(client, ModelPath);
        using var refused = await DeleteAsync(client, "upload/Forum/a.zip");

        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Information, Message: $"Class model deleted: {ModelPath}. CorrelationId=corr-1" });
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Warning, Message: $"Class model delete refused: {FileStorageRefusals.NotAClassModelFile}. FilePath=upload/Forum/a.zip CorrelationId=corr-1" });
    }

    /// <summary>A failure inside the service is a bad request with a fixed text (never the exception's), logged at Error with the exception.</summary>
    [Fact]
    public async Task AServiceFailure_IsLoggedWithTheException_AndNotLeaked()
    {
        var service = new Mock<Interfaces.IClassModelStorageService>();
        service.Setup(s => s.SaveAsync(It.IsAny<string?>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("disk-secret"));
        service.Setup(s => s.Delete(It.IsAny<string?>())).Throws(new IOException("disk-secret"));
        using HttpClient client = baseFactory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            foreach (var d in services.Where(d => d.ServiceType == typeof(Interfaces.IClassModelStorageService)).ToList()) services.Remove(d);
            services.AddScoped(_ => service.Object);
            services.AddSingleton<ILogger<ClassModelController>>(_logger);
        })).CreateClient();

        using var save = await SaveAsync(client, ModelPath, [1]);
        using var delete = await DeleteAsync(client, ModelPath);

        foreach (HttpResponseMessage response in new[] { save, delete })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.DoesNotContain("disk-secret", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        }
        Assert.Equal(2, _logger.Collector.GetSnapshot().Count(r => r is { Level: LogLevel.Error, Exception.Message: "disk-secret" }));
    }
}
