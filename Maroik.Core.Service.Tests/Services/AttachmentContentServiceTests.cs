using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Primitives;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AttachmentContentService"/>.
/// All external dependencies (file client, data protection, image validation, HTML sanitizing/parsing)
/// are replaced with Moq mocks. Covers attachment validation, Summernote image upload,
/// file download, and HTML display/persist transforms.
/// </summary>
public class AttachmentContentServiceTests
{
    /// <summary>Mock <c>IFileClient</c> injected into the system under test.</summary>
    private readonly Mock<IFileClient> _fileClient = new();
    /// <summary>Mock <c>IDataProtectionService</c> injected into the system under test.</summary>
    private readonly Mock<IDataProtectionService> _dataProtection = new();
    /// <summary>Mock <c>IImageValidatorService</c> injected into the system under test.</summary>
    private readonly Mock<IImageValidatorService> _imageValidator = ImageValidatorMock.Create();
    /// <summary>Mock <c>IHtmlContentSanitizerService</c> injected into the system under test.</summary>
    private readonly Mock<IHtmlContentSanitizerService> _htmlSanitizer = new();
    /// <summary>Mock <c>IHtmlParserService</c> injected into the system under test.</summary>
    private readonly Mock<IHtmlParserService> _htmlParser = new();
    /// <summary>Captures the log entries the system under test writes.</summary>
    private readonly FakeLogger<AttachmentContentService> _logger = new();
    /// <summary>Settings with a 1 KB attachment limit and a fake file-storage URL.</summary>
    private readonly IOptions<ServerSetting> _settings = Options.Create(new ServerSetting
    {
        MaxAttachedFileSizeBytes = 1024,
        FileStorageBaseUrl = "http://filestorage.local"
    });

    /// <summary>The service under test over the mocked dependencies.</summary>
    private AttachmentContentService CreateSut() => new(
        _fileClient.Object,
        _dataProtection.Object,
        _imageValidator.Object,
        _htmlSanitizer.Object,
        _htmlParser.Object,
        _settings,
        _logger);

    // -- ValidateAttachedFile ---------------------------------------------------

    /// <summary>Verifies that a null file is treated as "nothing to validate".</summary>
    [Fact]
    public void ValidateAttachedFile_ReturnsNull_WhenFileIsNull()
    {
        var result = CreateSut().ValidateAttachedFile(null);

        Assert.Null(result);
    }

    /// <summary>Verifies that non-zip extensions are rejected.</summary>
    [Fact]
    public void ValidateAttachedFile_ReturnsFail_WhenExtensionIsNotZip()
    {
        var file = new AttachedFileDto { FileName = "document.pdf", Size = 100 };

        var result = CreateSut().ValidateAttachedFile(file);

        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    /// <summary>Verifies that files larger than the configured limit are rejected.</summary>
    [Fact]
    public void ValidateAttachedFile_ReturnsFail_WhenSizeExceedsMax()
    {
        var file = new AttachedFileDto { FileName = "archive.zip", Size = 2048 };

        var result = CreateSut().ValidateAttachedFile(file);

        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    /// <summary>
    /// The "too large" message is a composite-format template with the configured max size (in MB)
    /// as a <see cref="ServiceResult.ErrorArgs"/> value -- not baked into
    /// <see cref="ServiceResult.ErrorKey"/> -- so every caller can localize it via
    /// <c>_localizer[result.ErrorKey, result.ErrorArgs]</c> instead of separately recomputing
    /// maxBytes/(1024*1024) at the call site.
    /// </summary>
    [Fact]
    public void ValidateAttachedFile_ReturnsDomainErrorArgs_WhenSizeExceedsMax()
    {
        var settings = Options.Create(new ServerSetting { MaxAttachedFileSizeBytes = 5 * 1024 * 1024, FileStorageBaseUrl = "http://filestorage.local" });
        var sut = new AttachmentContentService(
            _fileClient.Object, _dataProtection.Object, _imageValidator.Object, _htmlSanitizer.Object, _htmlParser.Object,
            settings, _logger);
        var file = new AttachedFileDto { FileName = "archive.zip", Size = 10 * 1024 * 1024 };

        var result = sut.ValidateAttachedFile(file);

        Assert.NotNull(result);
        Assert.Equal("File Size must be smaller than {0}MB.", result.ErrorKey);
        Assert.Equal([5L], result.ErrorArgs);
    }

    /// <summary>Verifies that a zero-byte file is rejected.</summary>
    [Fact]
    public void ValidateAttachedFile_ReturnsFail_WhenSizeIsZero()
    {
        var file = new AttachedFileDto { FileName = "archive.zip", Size = 0 };

        var result = CreateSut().ValidateAttachedFile(file);

        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    /// <summary>Verifies that a zip file within the size limit passes validation.</summary>
    [Fact]
    public void ValidateAttachedFile_ReturnsNull_WhenValid()
    {
        var file = new AttachedFileDto { FileName = "archive.zip", Size = 512 };

        var result = CreateSut().ValidateAttachedFile(file);

        Assert.Null(result);
    }

    /// <summary>Verifies that the zip extension check is case-insensitive (e.g. Windows-style "Archive.ZIP").</summary>
    [Fact]
    public void ValidateAttachedFile_ReturnsNull_WhenExtensionIsUppercaseZip()
    {
        var file = new AttachedFileDto { FileName = "Archive.ZIP", Size = 512 };

        var result = CreateSut().ValidateAttachedFile(file);

        Assert.Null(result);
    }

    // -- UploadSummernoteImageAsync ----------------------------------------------

    /// <summary>Verifies that an invalid image is rejected before ever calling the file client.</summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_ReturnsFail_WhenImageIsInvalid()
    {
        _imageValidator.Setup(v => v.IsValidImage(It.IsAny<byte[]>())).Returns(false);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = "image/png", FileName = "x.png" };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _fileClient.Verify(f => f.UploadAsync(
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>An empty image, or one past the limit, is refused with the size message and its limit, is logged, and is never stored.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1025)]
    public async Task UploadSummernoteImageAsync_RefusesASizeOutsideTheLimit(int size)
    {
        var settings = Options.Create(new ServerSetting { MaxAttachedFileSizeBytes = 1024 });
        var sut = new AttachmentContentService(_fileClient.Object, _dataProtection.Object, _imageValidator.Object, _htmlSanitizer.Object, _htmlParser.Object, settings, _logger);
        var file = new AttachedFileDto { Bytes = new byte[size], ContentType = "image/png", FileName = "x.png" };

        var result = await sut.UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorKeys.FileTooLarge, result.ErrorKey);
        Assert.Equal([0L], result.ErrorArgs);
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message.Contains("uploader@test.com", StringComparison.Ordinal));
        _imageValidator.Verify(v => v.IsValidImage(It.IsAny<byte[]>()), Times.Never);
        _fileClient.Verify(f => f.UploadAsync(
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// A file the Website did not read into memory because it is over the limit arrives with no bytes and its real size: it is
    /// refused by that size, before its name or type is looked at, and the log names the size.
    /// </summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_RefusesByTheDeclaredSize_AFileThatWasNotRead()
    {
        var settings = Options.Create(new ServerSetting { MaxAttachedFileSizeBytes = 1024 });
        var sut = new AttachmentContentService(_fileClient.Object, _dataProtection.Object, _imageValidator.Object, _htmlSanitizer.Object, _htmlParser.Object, settings, _logger);
        var unread = new AttachedFileDto { Bytes = [], ContentType = "", FileName = "big.gif", Size = 1025 };

        var result = await sut.UploadSummernoteImageAsync(unread, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorKeys.FileTooLarge, result.ErrorKey);
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message.Contains("1025 bytes", StringComparison.Ordinal));
    }

    /// <summary>An image exactly at the limit passes the size check.</summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_AcceptsAnImageAtTheLimit()
    {
        _imageValidator.Setup(v => v.IsValidImage(It.IsAny<byte[]>())).Returns(true);
        _fileClient.Setup(f => f.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var file = new AttachedFileDto { Bytes = new byte[1024], ContentType = "image/png", FileName = "x.png" };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    /// <summary>
    /// A file whose extension / content type is outside the jpg-png allow-list is rejected before the decode check: a wrong extension
    /// with the message naming the allowed ones, a wrong content type under an allowed extension as an invalid image.
    /// </summary>
    [Theory]
    [InlineData("photo.gif", "image/gif", ServiceErrorKeys.ImageFileExtensionNotAllowed)]
    [InlineData("photo.svg", "image/svg+xml", ServiceErrorKeys.ImageFileExtensionNotAllowed)]
    [InlineData("photo.bmp", "image/png", ServiceErrorKeys.ImageFileExtensionNotAllowed)]
    [InlineData("photo.png", "text/html", ServiceErrorKeys.InvalidImageFile)]
    public async Task UploadSummernoteImageAsync_ReturnsFail_WhenExtensionOrContentTypeNotAllowed(string fileName, string contentType, string expectedKey)
    {
        _imageValidator.Setup(v => v.IsValidImage(It.IsAny<byte[]>())).Returns(true);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = contentType, FileName = fileName };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(expectedKey, result.ErrorKey);
        _fileClient.Verify(f => f.UploadAsync(
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A jpg/png-labeled file that decodes as SVG is rejected (SVG can carry script).</summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_ReturnsFail_WhenBytesAreSvg()
    {
        _imageValidator.Setup(v => v.IsValidImage(It.IsAny<byte[]>())).Returns(true);
        _imageValidator.Setup(v => v.IsSvg(It.IsAny<byte[]>())).Returns(true);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = "image/png", FileName = "x.png" };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _fileClient.Verify(f => f.UploadAsync(
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Verifies that a failed upload short-circuits before attempting the re-download.</summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_ReturnsFail_WhenUploadFails()
    {
        _imageValidator.Setup(v => v.IsValidImage(It.IsAny<byte[]>())).Returns(true);
        _fileClient.Setup(f => f.UploadAsync(
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = "image/png", FileName = "x.png" };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        _fileClient.Verify(f => f.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// A stored editor image is returned from the bytes already in hand: the upload's success means
    /// file storage kept them as sent, so nothing is downloaded back.
    /// </summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_ReturnsTheUploadedBytes_WithoutDownloadingThemBack()
    {
        _imageValidator.Setup(v => v.IsValidImage(It.IsAny<byte[]>())).Returns(true);
        _imageValidator.Setup(v => v.StripMetadata(It.IsAny<byte[]>())).Returns<byte[]>(bytes => new StrippedImage(bytes, ".png", "image/png"));
        string? storedPath = null;
        _fileClient.Setup(f => f.UploadAsync(
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<byte[], string, string, CancellationToken>((_, _, path, _) => storedPath = path)
            .ReturnsAsync(true);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = "image/png", FileName = "x.png" };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(file.Bytes, result.FileBytes);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(storedPath, result.FilePath);
        _fileClient.Verify(f => f.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// The editor image is stored — and handed back to the editor — as the validator's metadata-free
    /// re-encoding, so the EXIF of a phone photo (GPS position, camera, capture time) never reaches the
    /// readers of the post or shared calendar.
    /// </summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_StoresAndReturnsTheMetadataFreeImage_NotTheUploadedBytes()
    {
        byte[] uploadedBytes = [0xFF, 0xD8, 0xFF, 0xE1, 0x45, 0x78, 0x69, 0x66];
        byte[] strippedBytes = [0xFF, 0xD8, 0xFF, 0xDB];
        _imageValidator.Setup(v => v.IsValidImage(uploadedBytes)).Returns(true);
        _imageValidator.Setup(v => v.StripMetadata(uploadedBytes)).Returns(new StrippedImage(strippedBytes, ".jpg", "image/jpeg"));
        byte[]? stored = null;
        _fileClient.Setup(f => f.UploadAsync(
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<byte[], string, string, CancellationToken>((bytes, _, _, _) => stored = bytes)
            .ReturnsAsync(true);
        var file = new AttachedFileDto { Bytes = uploadedBytes, ContentType = "image/jpeg", FileName = "photo.jpg" };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(strippedBytes, stored);
        Assert.Equal(strippedBytes, result.FileBytes);
    }

    /// <summary>
    /// An editor image is stored, uploaded and handed back under the format it actually is: a JPEG uploaded as
    /// "photo.png" / image/png is kept as a .jpg with image/jpeg, never under the name it claimed.
    /// </summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_StoresTheImageUnderTheFormatItActuallyIs()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xDB];
        _imageValidator.Setup(v => v.IsValidImage(jpeg)).Returns(true);
        string? storedPath = null;
        string? storedType = null;
        _fileClient.Setup(f => f.UploadAsync(
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<byte[], string, string, CancellationToken>((_, type, path, _) => (storedType, storedPath) = (type, path))
            .ReturnsAsync(true);
        var file = new AttachedFileDto { Bytes = jpeg, ContentType = "image/png", FileName = "photo.png" };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.EndsWith(".jpg", storedPath, StringComparison.Ordinal);
        Assert.Equal("image/jpeg", storedType);
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.EndsWith(".jpg", result.FileName, StringComparison.Ordinal);
    }

    /// <summary>A refused editor image is never re-encoded: the decoder only sees bytes the validator accepted.</summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_DoesNotReEncode_AnImageThatFailedValidation()
    {
        _imageValidator.Setup(v => v.IsValidImage(It.IsAny<byte[]>())).Returns(false);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = "image/png", FileName = "x.png" };

        await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        _imageValidator.Verify(v => v.StripMetadata(It.IsAny<byte[]>()), Times.Never);
    }

    // -- OpenFileAsync -------------------------------------------------------------

    /// <summary>The stream the file client opened is handed back unread.</summary>
    [Fact]
    public async Task OpenFileAsync_ReturnsTheOpenedStream()
    {
        var stream = new MemoryStream([1, 2, 3]);
        _fileClient.Setup(f => f.OpenReadAsync("upload/a.zip", It.IsAny<CancellationToken>())).ReturnsAsync(stream);

        Assert.Same(stream, await CreateSut().OpenFileAsync("upload/a.zip", TestContext.Current.CancellationToken));
    }

    /// <summary>A file the storage service cannot hand out yields null and one Error entry naming the path, with the exception attached.</summary>
    [Fact]
    public async Task OpenFileAsync_ReturnsNullAndLogsTheFailure_WhenTheFileCannotBeOpened()
    {
        var logger = new FakeLogger<AttachmentContentService>();
        var failure = new HttpRequestException("404");
        _fileClient.Setup(f => f.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var sut = new AttachmentContentService(_fileClient.Object, _dataProtection.Object, _imageValidator.Object, _htmlSanitizer.Object,
            _htmlParser.Object, _settings, logger);

        Assert.Null(await sut.OpenFileAsync("upload/a.zip", TestContext.Current.CancellationToken));

        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Contains("upload/a.zip", record.Message, StringComparison.Ordinal);
        Assert.Same(failure, record.Exception);
    }

    // -- PrepareHtmlForDisplayAsync -------------------------------------------------

    /// <summary>
    /// Verifies the patch-factory callback passed to <see cref="IHtmlParserService"/>: it should
    /// download the image at the given (encrypted) path and re-encrypt that same path for the
    /// new alt attribute, embedding the downloaded bytes as base64 with the correct content type.
    /// </summary>
    [Fact]
    public async Task PrepareHtmlForDisplayAsync_PatchFactory_DownloadsAndEncryptsAlt()
    {
        Func<string, CancellationToken, Task<HtmlImgPatch?>>? capturedFactory = null;
        _htmlParser.Setup(p => p.TransformImageAttributesAsync(
                It.IsAny<string>(), It.IsAny<Func<string, CancellationToken, Task<HtmlImgPatch?>>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken>(
                (_, factory, _) => capturedFactory = factory)
            .ReturnsAsync(("<html/>", true));
        byte[] fileData = [5, 6, 7];
        _fileClient.Setup(f => f.DownloadAsync("upload/img.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileData);
        _dataProtection.Setup(r => r.Protect(ProtectedValue.StoredFilePath, "upload/img.png")).Returns("encrypted-path");

        (string html, bool hasImages) = await CreateSut().PrepareHtmlForDisplayAsync("<html/>", TestContext.Current.CancellationToken);

        Assert.NotNull(capturedFactory);
        HtmlImgPatch? patch = await capturedFactory!("upload/img.png", TestContext.Current.CancellationToken);
        Assert.Equal("encrypted-path", patch!.NewAlt);
        Assert.Equal(Convert.ToBase64String(fileData), patch.DataFile);
        Assert.Equal("image/png", patch.DataContentType);
        Assert.True(hasImages);
        Assert.Equal("<html/>", html);
    }

    /// <summary>
    /// When the file-storage download fails (transient outage, missing blob, …) the patch must
    /// mark the tag for removal rather than returning null: a null patch leaves the tag's `alt`
    /// untouched, which is still the plaintext storage path at that point, so it would leak the
    /// internal storage layout to the client instead of degrading gracefully.
    /// </summary>
    [Fact]
    public async Task PrepareHtmlForDisplayAsync_PatchFactory_MarksImageForRemoval_WhenDownloadFails()
    {
        Func<string, CancellationToken, Task<HtmlImgPatch?>>? capturedFactory = null;
        _htmlParser.Setup(p => p.TransformImageAttributesAsync(
                It.IsAny<string>(), It.IsAny<Func<string, CancellationToken, Task<HtmlImgPatch?>>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken>(
                (_, factory, _) => capturedFactory = factory)
            .ReturnsAsync(("<html/>", true));
        _fileClient.Setup(f => f.DownloadAsync("upload/img.png", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage unreachable"));

        await CreateSut().PrepareHtmlForDisplayAsync("<html/>", TestContext.Current.CancellationToken);

        Assert.NotNull(capturedFactory);
        HtmlImgPatch? patch = await capturedFactory!("upload/img.png", TestContext.Current.CancellationToken);
        Assert.NotNull(patch);
        Assert.True(patch.Remove);
        Assert.Null(patch.NewAlt);
    }

    // -- SanitizeAndDecryptContent ---------------------------------------------------

    /// <summary>
    /// Verifies that content is sanitized first, then handed to the parser with a patch factory
    /// that decrypts each image's stored (encrypted) alt path.
    /// </summary>
    [Fact]
    public void SanitizeAndDecryptContent_SanitizesThenDecryptsAlt()
    {
        _htmlSanitizer.Setup(s => s.Sanitize("<raw/>")).Returns("<clean/>");
        Func<string, HtmlImgPatch?>? capturedFactory = null;
        _htmlParser.Setup(p => p.TransformImageAttributes("<clean/>", It.IsAny<Func<string, HtmlImgPatch?>>()))
            .Callback<string, Func<string, HtmlImgPatch?>>((_, factory) => capturedFactory = factory)
            .Returns("<final/>");
        _dataProtection.Setup(r => r.Unprotect(ProtectedValue.StoredFilePath, "encrypted-alt")).Returns("decrypted-alt");

        string result = CreateSut().SanitizeAndDecryptContent("<raw/>");

        Assert.Equal("<final/>", result);
        Assert.NotNull(capturedFactory);
        HtmlImgPatch? patch = capturedFactory!("encrypted-alt");
        Assert.Equal("decrypted-alt", patch!.NewAlt);
        Assert.False(patch.Remove);
    }

    /// <summary>
    /// An <c>&lt;img&gt;</c> whose <c>alt</c> is not a decryptable storage token — e.g. a
    /// hand-crafted plaintext path posted straight to the write endpoint rather than produced by
    /// the editor — is marked for removal, not persisted. Otherwise, the stored tag would drive a
    /// server-side file fetch every time the post is rendered.
    /// </summary>
    [Fact]
    public void SanitizeAndDecryptContent_MarksImageForRemoval_WhenAltDoesNotDecrypt()
    {
        _htmlSanitizer.Setup(s => s.Sanitize("<raw/>")).Returns("<clean/>");
        Func<string, HtmlImgPatch?>? capturedFactory = null;
        _htmlParser.Setup(p => p.TransformImageAttributes("<clean/>", It.IsAny<Func<string, HtmlImgPatch?>>()))
            .Callback<string, Func<string, HtmlImgPatch?>>((_, factory) => capturedFactory = factory)
            .Returns("<final/>");
        _dataProtection.Setup(r => r.Unprotect(ProtectedValue.StoredFilePath, "upload/Forum/whatever.png")).Throws<FormatException>();

        CreateSut().SanitizeAndDecryptContent("<raw/>");

        Assert.NotNull(capturedFactory);
        HtmlImgPatch? patch = capturedFactory!("upload/Forum/whatever.png");
        Assert.True(patch!.Remove);
        Assert.Null(patch.NewAlt);
    }

    /// <summary>A bare <c>&lt;img&gt;</c> with no <c>alt</c> yields a null patch (left untouched), not a removal.</summary>
    [Fact]
    public void SanitizeAndDecryptContent_LeavesImage_WhenAltIsEmpty()
    {
        _htmlSanitizer.Setup(s => s.Sanitize("<raw/>")).Returns("<clean/>");
        Func<string, HtmlImgPatch?>? capturedFactory = null;
        _htmlParser.Setup(p => p.TransformImageAttributes("<clean/>", It.IsAny<Func<string, HtmlImgPatch?>>()))
            .Callback<string, Func<string, HtmlImgPatch?>>((_, factory) => capturedFactory = factory)
            .Returns("<final/>");

        CreateSut().SanitizeAndDecryptContent("<raw/>");

        Assert.NotNull(capturedFactory);
        Assert.Null(capturedFactory!(""));
    }

    // -- SanitizeContent -----------------------------------------------------------

    /// <summary>Verifies that content with no image handling is passed straight to the sanitizer.</summary>
    [Fact]
    public void SanitizeContent_DelegatesToSanitizer()
    {
        _htmlSanitizer.Setup(s => s.Sanitize("<raw/>")).Returns("<clean/>");

        string result = CreateSut().SanitizeContent("<raw/>");

        Assert.Equal("<clean/>", result);
    }

    /// <summary>The embedded image's content type follows its stored file's extension (unknown extensions are served as generic binary).</summary>
    [Theory]
    [InlineData("upload/a.jpg", "image/jpeg")]
    [InlineData("upload/a.JPEG", "image/jpeg")]
    [InlineData("upload/a.gif", "image/gif")]
    [InlineData("upload/a.webp", "image/webp")]
    [InlineData("upload/a.bmp", "application/octet-stream")]
    [InlineData("upload/noextension", "application/octet-stream")]
    public async Task PrepareHtmlForDisplayAsync_PatchFactory_PicksTheContentTypeFromTheExtension(string path, string expected)
    {
        Func<string, CancellationToken, Task<HtmlImgPatch?>>? capturedFactory = null;
        _htmlParser.Setup(p => p.TransformImageAttributesAsync(
                It.IsAny<string>(), It.IsAny<Func<string, CancellationToken, Task<HtmlImgPatch?>>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken>((_, factory, _) => capturedFactory = factory)
            .ReturnsAsync(("<html/>", true));
        _fileClient.Setup(f => f.DownloadAsync(path, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        _dataProtection.Setup(r => r.Protect(ProtectedValue.StoredFilePath, path)).Returns("enc");

        await CreateSut().PrepareHtmlForDisplayAsync("<html/>", TestContext.Current.CancellationToken);
        HtmlImgPatch? patch = await capturedFactory!(path, TestContext.Current.CancellationToken);

        Assert.Equal(expected, patch!.DataContentType);
    }

    // -- Exact results, storage address and the undecryptable-alt warning -------------------

    /// <summary>A non-zip attachment is the "only zip" Validation error.</summary>
    [Fact]
    public void ValidateAttachedFile_ReturnsExtensionNotAllowed_WithItsMessage()
    {
        ServiceResult? result = CreateSut().ValidateAttachedFile(new AttachedFileDto { FileName = "document.pdf", Size = 100 });

        Assert.Equal(ServiceErrorType.Validation, result?.ErrorType);
        Assert.Equal("Attachment.ExtensionNotAllowed", result?.ErrorCode);
        Assert.Equal("Only zip extension allowed.", result?.ErrorKey);
    }

    /// <summary>An oversized attachment is the "too large" Validation error.</summary>
    [Fact]
    public void ValidateAttachedFile_ReturnsTooLarge_WithItsCode()
    {
        ServiceResult? result = CreateSut().ValidateAttachedFile(new AttachedFileDto { FileName = "a.zip", Size = 4096 });

        Assert.Equal(ServiceErrorType.Validation, result?.ErrorType);
        Assert.Equal("Attachment.TooLarge", result?.ErrorCode);
    }

    /// <summary>
    /// A refused editor image under an allowed extension (SVG content, undecodable) answers "Invalid image file."; a refused
    /// extension has its own message (<see cref="UploadSummernoteImageAsync_ReturnsFail_WhenExtensionOrContentTypeNotAllowed"/>).
    /// </summary>
    [Theory]
    [InlineData("photo.png", "image/png", true, true)]
    [InlineData("photo.png", "image/png", false, false)]
    public async Task UploadSummernoteImageAsync_AnswersInvalidImageFile_ForEveryRefusal(string fileName, string contentType, bool svg, bool valid)
    {
        _imageValidator.Setup(v => v.IsSvg(It.IsAny<byte[]>())).Returns(svg);
        _imageValidator.Setup(v => v.IsValidImage(It.IsAny<byte[]>())).Returns(valid);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = contentType, FileName = fileName };

        SummernoteUploadResult result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Invalid image file.", result.ErrorKey);
    }

    /// <summary>An editor image is uploaded to the configured file-storage address.</summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_UploadsToTheConfiguredFileStorage()
    {
        _imageValidator.Setup(v => v.IsValidImage(It.IsAny<byte[]>())).Returns(true);
        var file = new AttachedFileDto { Bytes = [0xFF, 0xD8, 0xFF], ContentType = "image/png", FileName = "x.png" };

        await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        _fileClient.Verify(f => f.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>A body image is downloaded from the configured file-storage address.</summary>
    [Fact]
    public async Task PrepareHtmlForDisplayAsync_DownloadsFromTheConfiguredFileStorage()
    {
        Func<string, CancellationToken, Task<HtmlImgPatch?>>? capturedFactory = null;
        _htmlParser.Setup(p => p.TransformImageAttributesAsync(
                It.IsAny<string>(), It.IsAny<Func<string, CancellationToken, Task<HtmlImgPatch?>>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken>((_, factory, _) => capturedFactory = factory)
            .ReturnsAsync(("<html/>", true));
        _fileClient.Setup(f => f.DownloadAsync("upload/img.png", It.IsAny<CancellationToken>())).ReturnsAsync([1]);

        await CreateSut().PrepareHtmlForDisplayAsync("<html/>", TestContext.Current.CancellationToken);
        HtmlImgPatch? patch = await capturedFactory!("upload/img.png", TestContext.Current.CancellationToken);

        Assert.Equal("AQ==", patch?.DataFile);
    }

    /// <summary>An image alt that does not decrypt is dropped with one Warning carrying the decryption failure.</summary>
    [Fact]
    public void SanitizeAndDecryptContent_LogsAWarning_WhenAnAltDoesNotDecrypt()
    {
        _htmlSanitizer.Setup(s => s.Sanitize("<raw/>")).Returns("<clean/>");
        Func<string, HtmlImgPatch?>? capturedFactory = null;
        _htmlParser.Setup(p => p.TransformImageAttributes("<clean/>", It.IsAny<Func<string, HtmlImgPatch?>>()))
            .Callback<string, Func<string, HtmlImgPatch?>>((_, factory) => capturedFactory = factory)
            .Returns("<final/>");
        var thrown = new FormatException("not base64");
        _dataProtection.Setup(r => r.Unprotect(ProtectedValue.StoredFilePath, "forged")).Throws(thrown);

        CreateSut().SanitizeAndDecryptContent("<raw/>");
        capturedFactory!("forged");

        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Same(thrown, record.Exception);
        Assert.Equal("Ignoring an image alt that is not a decryptable storage token", record.Message);
    }

    /// <summary>
    /// The same image referenced several times is downloaded once: every repeat gets the first download's patch, so a body
    /// that repeats one large image costs one fetch (and one base64 string), not one per tag.
    /// </summary>
    [Fact]
    public async Task PrepareHtmlForDisplayAsync_PatchFactory_DownloadsARepeatedImageOnce()
    {
        Func<string, CancellationToken, Task<HtmlImgPatch?>>? capturedFactory = null;
        _htmlParser.Setup(p => p.TransformImageAttributesAsync(
                It.IsAny<string>(), It.IsAny<Func<string, CancellationToken, Task<HtmlImgPatch?>>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken>(
                (_, factory, _) => capturedFactory = factory)
            .ReturnsAsync(("<html/>", true));
        _fileClient.Setup(f => f.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        _dataProtection.Setup(r => r.Protect(ProtectedValue.StoredFilePath, It.IsAny<string>())).Returns((ProtectedValue _, string path) => $"protected:{path}");

        await CreateSut().PrepareHtmlForDisplayAsync("<html/>", TestContext.Current.CancellationToken);
        HtmlImgPatch? first = await capturedFactory!("upload/a.png", TestContext.Current.CancellationToken);
        HtmlImgPatch? repeat = await capturedFactory("upload/a.png", TestContext.Current.CancellationToken);
        HtmlImgPatch? other = await capturedFactory("upload/b.png", TestContext.Current.CancellationToken);

        Assert.Same(first, repeat);
        Assert.Equal("protected:upload/b.png", other!.NewAlt);
        _fileClient.Verify(f => f.DownloadAsync("upload/a.png", It.IsAny<CancellationToken>()), Times.Once);
        _fileClient.Verify(f => f.DownloadAsync("upload/b.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>A repeat is not carried over to the next call: each display prepares from what file storage holds then.</summary>
    [Fact]
    public async Task PrepareHtmlForDisplayAsync_DownloadsAgain_OnTheNextCall()
    {
        var factories = new List<Func<string, CancellationToken, Task<HtmlImgPatch?>>>();
        _htmlParser.Setup(p => p.TransformImageAttributesAsync(
                It.IsAny<string>(), It.IsAny<Func<string, CancellationToken, Task<HtmlImgPatch?>>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken>(
                (_, factory, _) => factories.Add(factory))
            .ReturnsAsync(("<html/>", true));
        _fileClient.Setup(f => f.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([1]);
        AttachmentContentService sut = CreateSut();

        await sut.PrepareHtmlForDisplayAsync("<html/>", TestContext.Current.CancellationToken);
        await sut.PrepareHtmlForDisplayAsync("<html/>", TestContext.Current.CancellationToken);
        await factories[0]("upload/a.png", TestContext.Current.CancellationToken);
        await factories[1]("upload/a.png", TestContext.Current.CancellationToken);

        _fileClient.Verify(f => f.DownloadAsync("upload/a.png", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // -- CheckInlineImages ------------------------------------------------------------

    /// <summary>A body with as many images as the domain allows passes; one more is refused with the domain's error.</summary>
    [Fact]
    public void CheckInlineImages_AllowsTheLimit_AndRefusesOneMore()
    {
        _htmlParser.Setup(p => p.CountImages("at-limit")).Returns(TitledContentPolicy.MaxInlineImages);
        _htmlParser.Setup(p => p.CountImages("over-limit")).Returns(TitledContentPolicy.MaxInlineImages + 1);

        Assert.Null(CreateSut().CheckInlineImages("at-limit"));
        ServiceResult? refused = CreateSut().CheckInlineImages("over-limit");

        Assert.NotNull(refused);
        Assert.Equal(("Content.TooManyImages", ServiceErrorType.Validation), (refused.ErrorCode, refused.ErrorType));
        Assert.Equal("You can insert at most {0} images.", refused.ErrorKey);
        Assert.Equal([TitledContentPolicy.MaxInlineImages], refused.ErrorArgs);
    }
}
