using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Core.Domain.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IAttachmentContentService"/>. Files are virus-scanned by
/// ClamAV and image-validated before upload, and stored in/retrieved from the file-storage
/// microservice (<see cref="IFileClient"/>).
/// </summary>
public class AttachmentContentService : IAttachmentContentService
{
    private readonly IFileClient _fileClient;
    private readonly IRsaService _rsa;
    private readonly IImageValidatorService _imageValidator;
    private readonly IHtmlContentSanitizerService _htmlSanitizer;
    private readonly IHtmlParserService _htmlParser;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<AttachmentContentService> _logger;

    /// <summary>Initializes a new instance of <see cref="AttachmentContentService"/> with the supplied dependencies.</summary>
    public AttachmentContentService(
        IFileClient fileClient,
        IRsaService rsa,
        IImageValidatorService imageValidator,
        IHtmlContentSanitizerService htmlSanitizer,
        IHtmlParserService htmlParser,
        IOptions<ServerSetting> settings,
        ILogger<AttachmentContentService> logger)
    {
        _fileClient = fileClient;
        _rsa = rsa;
        _imageValidator = imageValidator;
        _htmlSanitizer = htmlSanitizer;
        _htmlParser = htmlParser;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public ServiceResult? ValidateAttachedFile(AttachedFileDto? attachedFile)
    {
        if (attachedFile == null) return null;
        if (!AttachmentUploadPolicy.IsAllowedExtension(Path.GetExtension(attachedFile.FileName)))
            return ServiceResult.Fail("Only zip extension allowed.");
        long maxBytes = _settings.Value.MaxAttachedFileSizeBytes;
        if (!AttachmentUploadPolicy.IsValidSize(attachedFile.Size, maxBytes))
            // Stable, value-independent key (same one ManagementController/ForumController/
            // CalendarController already use for the avatar/Summernote-image size errors) so the
            // resx lookup still matches when MaxAttachedFileSizeBytes is configured away from
            // 10MB. Callers that display this to the user must pass maxBytes/(1024*1024) as the
            // localizer's format argument instead of reading a pre-baked number out of the key.
            return ServiceResult.Fail("File Size must be smaller than {0}MB.");
        return null;
    }

    /// <inheritdoc />
    public async Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileDto file, string area, string subArea, CancellationToken ct = default)
    {
        if (!_imageValidator.IsValidImage(file.Bytes))
            return SummernoteUploadResult.Fail("Invalid image file.");

        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        // Use a GUID-based file name to avoid collisions and prevent path traversal via the original name.
        string imageFile = $"{Guid.NewGuid():N}{ext}";
        string filePath = Path.Combine("upload", area, subArea, "summernote", "images", imageFile);

        bool uploaded = await _fileClient.UploadAsync(file.Bytes, file.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? "", ct);
        if (!uploaded)
            return SummernoteUploadResult.Fail("Input is invalid");

        try
        {
            byte[] fileBytes = await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? "", ct);
            return SummernoteUploadResult.Ok(fileBytes, file.ContentType, imageFile, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file {FilePath} immediately after upload", filePath);
            return SummernoteUploadResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> DownloadFileAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            return await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? "", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file {FilePath}", filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<(string Html, bool HasImages)> PrepareHtmlForDisplayAsync(string html, CancellationToken ct = default)
    {
        return await _htmlParser.TransformImageAttributesAsync(html, async (alt, token) =>
        {
            byte[] fileData = await _fileClient.DownloadAsync(alt, _settings.Value.FileStorageBaseUrl ?? "", token);
            return new HtmlImgPatch(
                NewAlt: _rsa.Encrypt(alt),
                DataFile: Convert.ToBase64String(fileData),
                DataContentType: GetImageContentType(alt));
        }, ct);
    }

    /// <inheritdoc />
    public string SanitizeAndDecryptContent(string html)
    {
        string sanitized = _htmlSanitizer.Sanitize(html);
        return _htmlParser.TransformImageAttributes(sanitized, alt => new HtmlImgPatch(NewAlt: _rsa.Decrypt(alt)));
    }

    /// <inheritdoc />
    public string SanitizeContent(string html) => _htmlSanitizer.Sanitize(html);

    /// <summary>Returns the MIME content type for Summernote-uploaded images by file extension.</summary>
    private static string GetImageContentType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
}
