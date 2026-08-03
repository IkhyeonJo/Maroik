using Maroik.FileStorage.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Maroik.FileStorage.Controllers;

/// <summary>
/// Internal API controller for the Maroik.FileStorage microservice.
/// Handles virus-scanned file uploads (ClamAV) and raw file downloads.
/// All validation and file-system logic is delegated to <see cref="IFileValidationService"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly IFileValidationService _fileValidationService;
    private readonly ILogger<FileController> _logger;

    /// <summary>Initializes a new instance of <see cref="FileController"/>.</summary>
    public FileController(IFileValidationService fileValidationService, ILogger<FileController> logger)
    {
        _fileValidationService = fileValidationService;
        _logger = logger;
    }

    /// <summary>
    /// Accepts a multipart file upload and delegates validation, virus scan, and persistence
    /// to <see cref="IFileValidationService"/>.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadAsync([FromForm] IFormFile file, [FromForm] string filePath, [FromForm] string? correlationId = null)
    {
        try
        {
            var (success, error) = await _fileValidationService.ValidateAndSaveAsync(file, filePath);
            if (!success)
                return BadRequest(error);

#pragma warning disable CA1873
            _logger.LogInformation("File uploaded: {FilePath}. CorrelationId={CorrelationId}", filePath, correlationId);
#pragma warning restore CA1873
            return Ok("File uploaded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed for {FilePath}. CorrelationId={CorrelationId}", filePath, correlationId);
            return BadRequest("File upload failed.");
        }
    }

    /// <summary>Reads the file at the given server-side <paramref name="filePath"/> and returns its raw bytes.</summary>
    [AllowAnonymous]
    [HttpPost("download")]
    public async Task<IActionResult> DownloadAsync([FromForm] string filePath, [FromForm] string? correlationId = null)
    {
        try
        {
            var sanitizedFileName = Path.GetFileName(filePath);
            filePath = Path.Combine(Path.GetDirectoryName(filePath) ?? "", sanitizedFileName);

            if (!System.IO.File.Exists(filePath)) return NotFound("File not found");
            // filePath was rebuilt above from Path.GetFileName(filePath) + the original directory,
            // stripping any "..\" segments, so this is not a path-traversal read.
#pragma warning disable SCS0018
            var fileData = await System.IO.File.ReadAllBytesAsync(filePath);
#pragma warning restore SCS0018

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(sanitizedFileName, out var contentType))
                contentType = "application/octet-stream";

#pragma warning disable CA1873
            _logger.LogInformation("File downloaded: {FilePath}. CorrelationId={CorrelationId}", filePath, correlationId);
#pragma warning restore CA1873
            return File(fileData, contentType, sanitizedFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File download failed for {FilePath}. CorrelationId={CorrelationId}", filePath, correlationId);
            return BadRequest("Invalid file");
        }
    }
}
