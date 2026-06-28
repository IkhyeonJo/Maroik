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

    /// <summary>Initializes a new instance of <see cref="FileController"/>.</summary>
    public FileController(IFileValidationService fileValidationService)
    {
        _fileValidationService = fileValidationService;
    }

    /// <summary>
    /// Accepts a multipart file upload and delegates validation, virus scan, and persistence
    /// to <see cref="IFileValidationService"/>.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadAsync([FromForm] IFormFile file, [FromForm] string filePath)
    {
        try
        {
            var (success, error) = await _fileValidationService.ValidateAndSaveAsync(file, filePath);
            return success ? Ok("File uploaded successfully.") : BadRequest(error);
        }
        catch
        {
            return BadRequest("File upload failed.");
        }
    }

    /// <summary>Reads the file at the given server-side <paramref name="filePath"/> and returns its raw bytes.</summary>
    [AllowAnonymous]
    [HttpPost("download")]
    public async Task<IActionResult> DownloadAsync([FromForm] string filePath)
    {
        try
        {
            var sanitizedFileName = Path.GetFileName(filePath);
            filePath = Path.Combine(Path.GetDirectoryName(filePath ?? "") ?? string.Empty, sanitizedFileName);

            if (!System.IO.File.Exists(filePath)) return NotFound("File not found");
#pragma warning disable SCS0018
            var fileData = await System.IO.File.ReadAllBytesAsync(filePath);
#pragma warning restore SCS0018

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(sanitizedFileName, out var contentType))
                contentType = "application/octet-stream";

            return File(fileData, contentType, sanitizedFileName);
        }
        catch
        {
            return BadRequest("Invalid file");
        }
    }
}
