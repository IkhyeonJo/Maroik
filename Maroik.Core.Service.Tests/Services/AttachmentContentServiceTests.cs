using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AttachmentContentService"/>.
/// All external dependencies (file client, data protection, image validation, HTML sanitizing/parsing)
/// are replaced with hand-written fakes. Covers attachment validation, Summernote image upload,
/// file download, and HTML display/persist transforms.
/// </summary>
public class AttachmentContentServiceTests
{
    /// <summary>Fake <c>IFileClient</c> injected into the system under test.</summary>
    private readonly FakeFileClient _fileClient = new();
    /// <summary>Fake <c>IDataProtectionService</c> injected into the system under test.</summary>
    private readonly FakeDataProtectionService _dataProtection = new();
    /// <summary>Fake <c>IImageValidatorService</c> injected into the system under test.</summary>
    private readonly FakeImageValidatorService _imageValidator = EchoingImageValidator.Create();
    /// <summary>Fake <c>IHtmlContentSanitizerService</c> injected into the system under test.</summary>
    private readonly FakeHtmlContentSanitizerService _htmlSanitizer = new();
    /// <summary>Fake <c>IHtmlParserService</c> injected into the system under test.</summary>
    private readonly FakeHtmlParserService _htmlParser = new();
    /// <summary>Captures the log entries the system under test writes.</summary>
    private readonly FakeLogger<AttachmentContentService> _logger = new();
    /// <summary>Settings with a 1 KB attachment limit and a fake file-storage URL.</summary>
    private readonly IOptions<ServerSetting> _settings = Options.Create(new ServerSetting
    {
        MaxAttachedFileSizeBytes = 1024,
        FileStorageBaseUrl = "http://filestorage.local"
    });

    /// <summary>The service under test over the fake dependencies.</summary>
    private AttachmentContentService CreateSut() => new(
        _fileClient,
        _dataProtection,
        _imageValidator,
        _htmlSanitizer,
        _htmlParser,
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
            _fileClient, _dataProtection, _imageValidator, _htmlSanitizer, _htmlParser,
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
        _imageValidator.AnswerIsValidImage(_ => false);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = "image/png", FileName = "x.png" };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Empty(_fileClient.UploadAsyncCalls);
    }

    /// <summary>A file whose extension / content type is outside the jpg-png allow-list is rejected before the decode check.</summary>
    [Theory]
    [InlineData("photo.gif", "image/gif")]
    [InlineData("photo.svg", "image/svg+xml")]
    [InlineData("photo.png", "text/html")]
    [InlineData("photo.bmp", "image/bmp")]
    public async Task UploadSummernoteImageAsync_ReturnsFail_WhenExtensionOrContentTypeNotAllowed(string fileName, string contentType)
    {
        _imageValidator.AnswerIsValidImage(_ => true);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = contentType, FileName = fileName };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Empty(_fileClient.UploadAsyncCalls);
    }

    /// <summary>A jpg/png-labeled file that decodes as SVG is rejected (SVG can carry script).</summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_ReturnsFail_WhenBytesAreSvg()
    {
        _imageValidator.AnswerIsValidImage(_ => true);
        _imageValidator.AnswerIsSvg(_ => true);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = "image/png", FileName = "x.png" };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Empty(_fileClient.UploadAsyncCalls);
    }

    /// <summary>Verifies that a failed upload short-circuits before attempting the re-download.</summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_ReturnsFail_WhenUploadFails()
    {
        _imageValidator.AnswerIsValidImage(_ => true);
        _fileClient.AnswerUploadAsync((_, _, _, _, _) => Task.FromResult<bool>(false));
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = "image/png", FileName = "x.png" };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.Empty(_fileClient.DownloadAsyncCalls);
    }

    /// <summary>
    /// A stored editor image is returned from the bytes already in hand: the upload's success means
    /// file storage kept them as sent, so nothing is downloaded back.
    /// </summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_ReturnsTheUploadedBytes_WithoutDownloadingThemBack()
    {
        _imageValidator.AnswerIsValidImage(_ => true);
        _imageValidator.AnswerStripMetadata(bytes => new StrippedImage(bytes, ".png", "image/png"));
        string? storedPath = null;
        _fileClient.AnswerUploadAsync((_, _, path, _, _) => { storedPath = path; return Task.FromResult<bool>(true); });
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = "image/png", FileName = "x.png" };

        var result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(file.Bytes, result.FileBytes);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(storedPath, result.FilePath);
        Assert.Empty(_fileClient.DownloadAsyncCalls);
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
        _imageValidator.AnswerIsValidImage(bytesArg => bytesArg == uploadedBytes, _ => true);
        _imageValidator.AnswerStripMetadata(bytesArg => bytesArg == uploadedBytes, _ => new StrippedImage(strippedBytes, ".jpg", "image/jpeg"));
        byte[]? stored = null;
        _fileClient.AnswerUploadAsync((bytes, _, _, _, _) => { stored = bytes; return Task.FromResult<bool>(true); });
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
        _imageValidator.AnswerIsValidImage(bytesArg => bytesArg == jpeg, _ => true);
        string? storedPath = null;
        string? storedType = null;
        _fileClient.AnswerUploadAsync((_, type, path, _, _) => { (storedType, storedPath) = (type, path); return Task.FromResult<bool>(true); });
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
    public async Task UploadSummernoteImageAsync_DoesNotReencode_AnImageThatFailedValidation()
    {
        _imageValidator.AnswerIsValidImage(_ => false);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = "image/png", FileName = "x.png" };

        await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.Empty(_imageValidator.StripMetadataCalls);
    }

    // -- OpenFileAsync -------------------------------------------------------------

    /// <summary>The stream the file client opened is handed back unread.</summary>
    [Fact]
    public async Task OpenFileAsync_ReturnsTheOpenedStream()
    {
        var stream = new MemoryStream([1, 2, 3]);
        _fileClient.AnswerOpenReadAsync((filePath, fileStorageBaseUrl, _) => filePath == "upload/a.zip" && fileStorageBaseUrl == "http://filestorage.local", (_, _, _) => Task.FromResult<Stream>(stream));

        Assert.Same(stream, await CreateSut().OpenFileAsync("upload/a.zip", TestContext.Current.CancellationToken));
    }

    /// <summary>A file the storage service cannot hand out yields null and one Error entry naming the path, with the exception attached.</summary>
    [Fact]
    public async Task OpenFileAsync_ReturnsNullAndLogsTheFailure_WhenTheFileCannotBeOpened()
    {
        var logger = new Microsoft.Extensions.Logging.Testing.FakeLogger<AttachmentContentService>();
        var failure = new HttpRequestException("404");
        _fileClient.AnswerOpenReadAsync((_, _, _) => Task.FromException<Stream>(failure));
        var sut = new AttachmentContentService(_fileClient, _dataProtection, _imageValidator, _htmlSanitizer,
            _htmlParser, _settings, logger);

        Assert.Null(await sut.OpenFileAsync("upload/a.zip", TestContext.Current.CancellationToken));

        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Error, record.Level);
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
        _htmlParser.AnswerTransformImageAttributesAsync((_, factory, _) => { capturedFactory = factory; return Task.FromResult<(string, bool)>(("<html/>", true)); });
        byte[] fileData = [5, 6, 7];
        _fileClient.AnswerDownloadAsync((filePath, _, _) => filePath == "upload/img.png", (_, _, _) => Task.FromResult<byte[]>(fileData));
        _dataProtection.AnswerProtect((kind, plaintextArg) => kind == ProtectedValue.StoredFilePath && plaintextArg == "upload/img.png", (_, _) => "encrypted-path");

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
        _htmlParser.AnswerTransformImageAttributesAsync((_, factory, _) => { capturedFactory = factory; return Task.FromResult<(string, bool)>(("<html/>", true)); });
        _fileClient.AnswerDownloadAsync((filePath, _, _) => filePath == "upload/img.png", (_, _, _) => Task.FromException<byte[]>(new InvalidOperationException("storage unreachable")));

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
        _htmlSanitizer.AnswerSanitize(html => html == "<raw/>", _ => "<clean/>");
        Func<string, HtmlImgPatch?>? capturedFactory = null;
        _htmlParser.AnswerTransformImageAttributes((htmlArg, _) => htmlArg == "<clean/>", (_, factory) => { capturedFactory = factory; return "<final/>"; });
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.StoredFilePath && protectedValue == "encrypted-alt", (_, _) => "decrypted-alt");

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
        _htmlSanitizer.AnswerSanitize(htmlArg => htmlArg == "<raw/>", _ => "<clean/>");
        Func<string, HtmlImgPatch?>? capturedFactory = null;
        _htmlParser.AnswerTransformImageAttributes((htmlArg, _) => htmlArg == "<clean/>", (_, factory) => { capturedFactory = factory; return "<final/>"; });
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.StoredFilePath && protectedValue == "upload/Forum/whatever.png", (_, _) => throw new FormatException());

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
        _htmlSanitizer.AnswerSanitize(htmlArg => htmlArg == "<raw/>", _ => "<clean/>");
        Func<string, HtmlImgPatch?>? capturedFactory = null;
        _htmlParser.AnswerTransformImageAttributes((htmlArg, _) => htmlArg == "<clean/>", (_, factory) => { capturedFactory = factory; return "<final/>"; });

        CreateSut().SanitizeAndDecryptContent("<raw/>");

        Assert.NotNull(capturedFactory);
        Assert.Null(capturedFactory!(""));
    }

    // -- SanitizeContent -----------------------------------------------------------

    /// <summary>Verifies that content with no image handling is passed straight to the sanitizer.</summary>
    [Fact]
    public void SanitizeContent_DelegatesToSanitizer()
    {
        _htmlSanitizer.AnswerSanitize(htmlArg => htmlArg == "<raw/>", _ => "<clean/>");

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
        _htmlParser.AnswerTransformImageAttributesAsync((_, factory, _) => { capturedFactory = factory; return Task.FromResult<(string, bool)>(("<html/>", true)); });
        _fileClient.AnswerDownloadAsync((filePath, _, _) => filePath == path, (_, _, _) => Task.FromResult<byte[]>([1, 2, 3]));
        _dataProtection.AnswerProtect((kind, plaintext) => kind == ProtectedValue.StoredFilePath && plaintext == path, (_, _) => "enc");

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

    /// <summary>Every refused editor image (wrong type, SVG, undecodable) answers "Invalid image file.".</summary>
    [Theory]
    [InlineData("photo.gif", "image/gif", false, true)]
    [InlineData("photo.png", "image/png", true, true)]
    [InlineData("photo.png", "image/png", false, false)]
    public async Task UploadSummernoteImageAsync_AnswersInvalidImageFile_ForEveryRefusal(string fileName, string contentType, bool svg, bool valid)
    {
        _imageValidator.AnswerIsSvg(_ => svg);
        _imageValidator.AnswerIsValidImage(_ => valid);
        var file = new AttachedFileDto { Bytes = [1, 2, 3], ContentType = contentType, FileName = fileName };

        SummernoteUploadResult result = await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Invalid image file.", result.ErrorKey);
    }

    /// <summary>An editor image is uploaded to the configured file-storage address.</summary>
    [Fact]
    public async Task UploadSummernoteImageAsync_UploadsToTheConfiguredFileStorage()
    {
        _imageValidator.AnswerIsValidImage(_ => true);
        var file = new AttachedFileDto { Bytes = [0xFF, 0xD8, 0xFF], ContentType = "image/png", FileName = "x.png" };

        await CreateSut().UploadSummernoteImageAsync(file, "board", "post", "uploader@test.com", TestContext.Current.CancellationToken);

        Assert.Single(_fileClient.UploadAsyncCalls, call => call.FileStorageBaseUrl == "http://filestorage.local");
    }

    /// <summary>A body image is downloaded from the configured file-storage address.</summary>
    [Fact]
    public async Task PrepareHtmlForDisplayAsync_DownloadsFromTheConfiguredFileStorage()
    {
        Func<string, CancellationToken, Task<HtmlImgPatch?>>? capturedFactory = null;
        _htmlParser.AnswerTransformImageAttributesAsync((_, factory, _) => { capturedFactory = factory; return Task.FromResult<(string, bool)>(("<html/>", true)); });
        _fileClient.AnswerDownloadAsync((filePath, fileStorageBaseUrl, _) => filePath == "upload/img.png" && fileStorageBaseUrl == "http://filestorage.local", (_, _, _) => Task.FromResult<byte[]>([1]));

        await CreateSut().PrepareHtmlForDisplayAsync("<html/>", TestContext.Current.CancellationToken);
        HtmlImgPatch? patch = await capturedFactory!("upload/img.png", TestContext.Current.CancellationToken);

        Assert.Equal("AQ==", patch?.DataFile);
    }

    /// <summary>An image alt that does not decrypt is dropped with one Warning carrying the decryption failure.</summary>
    [Fact]
    public void SanitizeAndDecryptContent_LogsAWarning_WhenAnAltDoesNotDecrypt()
    {
        _htmlSanitizer.AnswerSanitize(html => html == "<raw/>", _ => "<clean/>");
        Func<string, HtmlImgPatch?>? capturedFactory = null;
        _htmlParser.AnswerTransformImageAttributes((html, _) => html == "<clean/>", (_, factory) => { capturedFactory = factory; return "<final/>"; });
        var thrown = new FormatException("not base64");
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.StoredFilePath && protectedValue == "forged", (_, _) => throw thrown);

        CreateSut().SanitizeAndDecryptContent("<raw/>");
        capturedFactory!("forged");

        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Same(thrown, record.Exception);
        Assert.Equal("Ignoring an image alt that is not a decryptable storage token", record.Message);
    }
}
