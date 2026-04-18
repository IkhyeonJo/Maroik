using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.FileStorage.Controllers;

/// <summary>
/// Internal API controller for the Maroik.FileStorage microservice.
/// Handles virus-scanned file uploads (ClamAV) and raw file downloads.
/// Supports .zip, .jpg, .jpeg, and .png files up to 10 MB.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly IClamavClient _clamavClient;

    /// <summary>Initializes a new instance of <see cref="FileController"/> with <paramref name="clamavClient"/>.</summary>
    public FileController(IClamavClient clamavClient)
    {
        _clamavClient = clamavClient;
    }

    /// <summary>
    /// Accepts a multipart file upload, validates the extension and size, scans for viruses via ClamAV,
    /// and writes the file to the server-side path specified by <paramref name="filePath"/>.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadAsync([FromForm] IFormFile file, [FromForm] string filePath)
    {
        try
        {
            if (file is not { Length: > 0 })
                return BadRequest("Invalid file.");

            var fileName = Path.GetFileName(file.FileName);
            var safeDirectory = Path.GetFullPath(filePath);
            var safeFilePath = Path.Combine(safeDirectory, fileName);
            
            // extension check
            var allowedExt = new[] { ".zip", ".jpg", ".jpeg", ".png" };
            if (!allowedExt.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
                return BadRequest("Unsupported file extension.");

            // file size check
            if (file.Length > 10 * 1024 * 1024) // 10MB
                return BadRequest("File too large.");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            
            bool IsAllowedFileType(Stream stream, string tempFileName)
            {
                stream.Position = 0;
                var buffer = new byte[8];
                _ = stream.Read(buffer, 0, buffer.Length);
                stream.Position = 0;

                var ext = Path.GetExtension(tempFileName).ToLowerInvariant();

                return ext switch
                {
                    ".zip" => buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
                    ".jpg" or ".jpeg" => buffer[0] == 0xFF && buffer[1] == 0xD8, // JPEG
                    ".png" => buffer.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                    _ => false
                };
            }

            // Magic Header validation by file type
            if (!IsAllowedFileType(memoryStream, fileName))
                return BadRequest("Invalid or unsupported file format.");
            
            // ClamAV virus scan
            if (!await _clamavClient.ScanWithClamavAsync(memoryStream, "Maroik.Clamav", 3310))
                return BadRequest("File may be infected with a virus.");

            // Check if encrypted for .zip extension
            if (Path.GetExtension(fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                // Reset stream position
                memoryStream.Position = 0;
                
                // Create stream copy (caution: increases memory usage)
                using var msCopy = new MemoryStream();
                await memoryStream.CopyToAsync(msCopy);
                msCopy.Position = 0;
                
                // Now check zip
                using var zipFile = new ZipFile(msCopy);
                foreach (ZipEntry entry in zipFile)
                {
                    if (entry.IsCrypted)
                        return BadRequest("Encrypted ZIP files are not allowed.");
                }
            }
            
            if (!Directory.Exists(safeDirectory))
                Directory.CreateDirectory(safeDirectory);

            // Save
    #pragma warning disable SCS0018
            await using var stream = new FileStream(safeFilePath, FileMode.Create);
    #pragma warning restore SCS0018
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(stream);
            await stream.FlushAsync();

            // Remove execute permission (chmod 644)
            var chmod = new ProcessStartInfo
            {
                FileName = "/bin/chmod",
    #pragma warning disable SCS0001
                Arguments = $"644 \"{safeFilePath}\"",
    #pragma warning restore SCS0001
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var chmodProcess = Process.Start(chmod);
            await chmodProcess!.WaitForExitAsync();

            return Ok("File uploaded successfully.");
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
            // Remove directory path from file name (security)
            var sanitizedFileName = Path.GetFileName(filePath);
            filePath = Path.Combine(Path.GetDirectoryName(filePath ?? "") ?? string.Empty, sanitizedFileName);

            // Check if file exists
            if (!System.IO.File.Exists(filePath)) return NotFound("File not found");
#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found.
            var fileData = await System.IO.File.ReadAllBytesAsync(filePath);
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found.

            // Use FileExtensionContentTypeProvider to get Content-Type matching file extension
            var provider = new FileExtensionContentTypeProvider();

            // Set contentType (default to "application/octet-stream" if not found)
            if (!provider.TryGetContentType(sanitizedFileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return File(fileData, contentType, sanitizedFileName);

        }
        catch
        {
            return BadRequest("Invalid file");
        }
    }
}