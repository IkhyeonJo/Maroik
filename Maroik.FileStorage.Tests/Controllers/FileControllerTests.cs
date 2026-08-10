using System.Net;
using System.Net.Http.Headers;
using Maroik.Core.Contract.Interfaces;
using Maroik.FileStorage.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Maroik.FileStorage.Tests.Controllers;

/// <summary>
/// Integration tests for <see cref="Maroik.FileStorage.Controllers.FileController"/>.
/// Uses <see cref="WebApplicationFactory{TEntryPoint}"/> to spin up the real ASP.NET pipeline
/// in memory, replacing <see cref="IClamavClient"/> with a controllable mock.
///
/// Note: the "happy-path" upload (file actually written to disk) is not tested here because
/// the action calls /bin/chmod which is Linux-only. All validation paths ?? extension,
/// size, magic bytes, ClamAV, and encrypted-ZIP checks ?? are fully covered.
/// </summary>
public class FileControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    /// <summary>Initializes a new instance of FileControllerTests, sharing the WebApplicationFactory&lt;Program&gt; fixture across the tests in this class.</summary>
    public FileControllerTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    // -- Factory helpers ------------------------------------------------------

    /// <summary>Returns a factory whose ClamAV mock always marks files as clean.</summary>
    private WebApplicationFactory<Program> FactoryWithCleanScan() =>
        _baseFactory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            RemoveService<IClamavClient>(services);
            var mock = new Mock<IClamavClient>();
            mock.Setup(c => c.ScanWithClamavAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(true);
            services.AddScoped<IClamavClient>(_ => mock.Object);
        }));

    /// <summary>Returns a factory whose ClamAV mock always reports a virus.</summary>
    private WebApplicationFactory<Program> FactoryWithVirusDetected() =>
        _baseFactory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            RemoveService<IClamavClient>(services);
            var mock = new Mock<IClamavClient>();
            mock.Setup(c => c.ScanWithClamavAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            services.AddScoped<IClamavClient>(_ => mock.Object);
        }));

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null) services.Remove(descriptor);
    }

    // -- Upload: invalid input (no ClamAV call needed) ------------------------

    /// <summary>Verifies that <c>Upload</c> returns bad request when no file is provided.</summary>
    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenNoFileIsProvided()
    {
        var client = FactoryWithCleanScan().CreateClient();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("/tmp/uploads"), "filePath");
        // Intentionally omit the "file" part

        var response = await client.PostAsync("/api/File/upload", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that <c>Upload</c> returns bad request when file is empty.</summary>
    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileIsEmpty()
    {
        var client = FactoryWithCleanScan().CreateClient();
        using var content = new MultipartFormDataContent();
        var emptyFile = new ByteArrayContent([]);
        emptyFile.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(emptyFile, "file", "empty.jpg");
        content.Add(new StringContent("/tmp/uploads"), "filePath");

        var response = await client.PostAsync("/api/File/upload", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that <c>Upload</c> returns bad request when extension is not allowed.</summary>
    [Theory]
    [InlineData("malware.exe")]
    [InlineData("script.sh")]
    [InlineData("archive.tar.gz")]
    [InlineData("document.pdf")]
    public async Task Upload_ReturnsBadRequest_WhenExtensionIsNotAllowed(string fileName)
    {
        var client = FactoryWithCleanScan().CreateClient();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0x01, 0x02, 0x03]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent("/tmp/uploads"), "filePath");

        var response = await client.PostAsync("/api/File/upload", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that <c>Upload</c> returns bad request when file size exceeds10 mb.</summary>
    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileSizeExceeds10MB()
    {
        var client = FactoryWithCleanScan().CreateClient();
        using var content = new MultipartFormDataContent();
        var largeBytes = new byte[11 * 1024 * 1024]; // 11 MB
        var fileContent = new ByteArrayContent(largeBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "large.jpg");
        content.Add(new StringContent("/tmp/uploads"), "filePath");

        var response = await client.PostAsync("/api/File/upload", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -- Upload: magic-byte validation ----------------------------------------

    /// <summary>Verifies that <c>Upload</c> returns bad request when jpg has wrong magic bytes.</summary>
    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenJpgHasWrongMagicBytes()
    {
        var client = FactoryWithCleanScan().CreateClient();
        using var content = new MultipartFormDataContent();
        // JPEG requires 0xFF 0xD8 as first two bytes
        var fileContent = new ByteArrayContent([0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "fake.jpg");
        content.Add(new StringContent("/tmp/uploads"), "filePath");

        var response = await client.PostAsync("/api/File/upload", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that <c>Upload</c> returns bad request when jpeg extension has wrong magic bytes.</summary>
    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenJpegExtensionHasWrongMagicBytes()
    {
        var client = FactoryWithCleanScan().CreateClient();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]); // PNG bytes, not JPEG
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "wrong.jpeg");
        content.Add(new StringContent("/tmp/uploads"), "filePath");

        var response = await client.PostAsync("/api/File/upload", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that <c>Upload</c> returns bad request when png has wrong magic bytes.</summary>
    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenPngHasWrongMagicBytes()
    {
        var client = FactoryWithCleanScan().CreateClient();
        using var content = new MultipartFormDataContent();
        // PNG requires: 89 50 4E 47 0D 0A 1A 0A ?? use wrong bytes
        var fileContent = new ByteArrayContent([0x89, 0x50, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "fake.png");
        content.Add(new StringContent("/tmp/uploads"), "filePath");

        var response = await client.PostAsync("/api/File/upload", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that <c>Upload</c> returns bad request when zip has wrong magic bytes.</summary>
    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenZipHasWrongMagicBytes()
    {
        var client = FactoryWithCleanScan().CreateClient();
        using var content = new MultipartFormDataContent();
        // ZIP requires: 50 4B 03 04 ?? use wrong bytes
        var fileContent = new ByteArrayContent([0x00, 0x00, 0x00, 0x00, 0x05, 0x06, 0x07, 0x08]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");
        content.Add(fileContent, "file", "fake.zip");
        content.Add(new StringContent("/tmp/uploads"), "filePath");

        var response = await client.PostAsync("/api/File/upload", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -- Upload: ClamAV scan --------------------------------------------------

    /// <summary>Verifies that <c>Upload</c> returns bad request when clam av reports virus.</summary>
    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenClamAvReportsVirus()
    {
        var client = FactoryWithVirusDetected().CreateClient();
        using var content = new MultipartFormDataContent();
        // Valid JPEG magic bytes so it passes the magic-byte check and reaches the scan
        var fileContent = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "infected.jpg");
        content.Add(new StringContent("/tmp/uploads"), "filePath");

        var response = await client.PostAsync("/api/File/upload", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -- Upload: encrypted ZIP ------------------------------------------------

    /// <summary>Verifies that <c>Upload</c> returns bad request when zip is encrypted.</summary>
    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenZipIsEncrypted()
    {
        // Create a minimal encrypted ZIP in memory using SharpZipLib conventions.
        // The encrypted flag bit (0x0001) in the local file header's general purpose flags indicates encryption.
        // We construct a valid ZIP local file header with the encryption bit set.
        // Format: PK\x03\x04 (4) | version (2) | flags=0x0001 (2, encryption bit) | ...
        byte[] encryptedZipBytes =
        [
            0x50, 0x4B, 0x03, 0x04, // Local file header signature
            0x14, 0x00,             // Version needed: 2.0
            0x01, 0x00,             // General purpose bit flag: bit 0 set = encrypted
            0x00, 0x00,             // Compression method: stored
            0x00, 0x00,             // Last mod time
            0x00, 0x00,             // Last mod date
            0x00, 0x00, 0x00, 0x00, // CRC-32
            0x04, 0x00, 0x00, 0x00, // Compressed size
            0x04, 0x00, 0x00, 0x00, // Uncompressed size
            0x04, 0x00,             // File name length
            0x00, 0x00,             // Extra field length
            0x74, 0x65, 0x73, 0x74, // File name: "test"
            0x01, 0x02, 0x03, 0x04, // Encrypted file data (dummy)
            // Central directory
            0x50, 0x4B, 0x01, 0x02,
            0x14, 0x00, 0x14, 0x00,
            0x01, 0x00,             // Encryption bit set in central directory too
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x74, 0x65, 0x73, 0x74, // File name
            // End of central directory
            0x50, 0x4B, 0x05, 0x06,
            0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x01, 0x00,
            0x32, 0x00, 0x00, 0x00,
            0x28, 0x00, 0x00, 0x00,
            0x00, 0x00
        ];

        var client = FactoryWithCleanScan().CreateClient();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(encryptedZipBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");
        content.Add(fileContent, "file", "encrypted.zip");
        content.Add(new StringContent("/tmp/uploads"), "filePath");

        var response = await client.PostAsync("/api/File/upload", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -- Upload: unexpected exception (logging path) --------------------------

    /// <summary>Verifies that <c>Upload</c> returns bad request — and doesn't fail DI resolution
    /// now that <see cref="Maroik.FileStorage.Controllers.FileController"/> takes an
    /// <c>ILogger</c> — when the validation service throws, with a correlationId form field present.</summary>
    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenValidationServiceThrows()
    {
        var factory = _baseFactory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            RemoveService<IFileValidationService>(services);
            var mock = new Mock<IFileValidationService>();
            mock.Setup(v => v.ValidateAndSaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("disk full"));
            services.AddScoped<IFileValidationService>(_ => mock.Object);
        }));
        var client = factory.CreateClient();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "test.jpg");
        content.Add(new StringContent("/tmp/uploads"), "filePath");
        content.Add(new StringContent("test-correlation-123"), "correlationId");

        var response = await client.PostAsync("/api/File/upload", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -- Download -------------------------------------------------------------

    /// <summary>Verifies that <c>Download</c> returns not found when file does not exist.</summary>
    [Fact]
    public async Task Download_ReturnsNotFound_WhenFileDoesNotExist()
    {
        var client = _baseFactory.CreateClient();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("/nonexistent/path/does_not_exist_xyz.jpg"), "filePath");

        var response = await client.PostAsync("/api/File/download", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that <c>Download</c> returns file bytes when file exists.</summary>
    [Fact]
    public async Task Download_ReturnsFileBytes_WhenFileExists()
    {
        // Arrange: write a small temp file to disk
        var tempPath = Path.Combine(Path.GetTempPath(), $"maroik_test_{Guid.NewGuid()}.jpg");
        byte[] jpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        await File.WriteAllBytesAsync(tempPath, jpegBytes, TestContext.Current.CancellationToken);

        try
        {
            var client = _baseFactory.CreateClient();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(tempPath), "filePath");

            var response = await client.PostAsync("/api/File/download", content, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseBytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
            Assert.Equal(jpegBytes, responseBytes);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    /// <summary>Verifies that <c>Download</c> returns correct content type for png.</summary>
    [Fact]
    public async Task Download_ReturnsCorrectContentType_ForPng()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"maroik_test_{Guid.NewGuid()}.png");
        byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        await File.WriteAllBytesAsync(tempPath, pngBytes, TestContext.Current.CancellationToken);

        try
        {
            var client = _baseFactory.CreateClient();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(tempPath), "filePath");

            var response = await client.PostAsync("/api/File/download", content, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
