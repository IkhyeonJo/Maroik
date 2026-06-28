using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;
using Maroik.Core.Contract.Interfaces;
using Maroik.FileStorage.Contracts;

namespace Maroik.FileStorage.Services;

/// <summary>
/// Validates uploaded files against extension, size, magic-byte, ZIP-encryption,
/// and ClamAV virus-scan rules before writing them to disk.
/// Extracted from <c>FileController</c> to keep the controller free of infrastructure concerns.
/// </summary>
public class FileValidationService : IFileValidationService
{
    private static readonly string[] AllowedExtensions = [".zip", ".jpg", ".jpeg", ".png"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IClamavClient _clamavClient;

    /// <summary>Initializes a new instance of <see cref="FileValidationService"/>.</summary>
    public FileValidationService(IClamavClient clamavClient)
    {
        _clamavClient = clamavClient;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? Error)> ValidateAndSaveAsync(IFormFile file, string filePath)
    {
        if (file is not { Length: > 0 })
            return (false, "Invalid file.");

        var fileName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var safeDir = Path.GetFullPath(filePath);
        var safePath = Path.Combine(safeDir, fileName);

        if (!AllowedExtensions.Contains(ext))
            return (false, "Unsupported file extension.");

        if (file.Length > MaxFileSizeBytes)
            return (false, "File too large.");

        using var memStream = new MemoryStream();
        await file.CopyToAsync(memStream);

        if (!HasValidMagicBytes(memStream, ext))
            return (false, "Invalid or unsupported file format.");

        if (!await _clamavClient.ScanWithClamavAsync(memStream, "Maroik.Clamav", 3310))
            return (false, "File may be infected with a virus.");

        if (ext == ".zip" && IsZipEncrypted(memStream))
            return (false, "Encrypted ZIP files are not allowed.");

        if (!Directory.Exists(safeDir))
            Directory.CreateDirectory(safeDir);

#pragma warning disable SCS0018
        await using var stream = new FileStream(safePath, FileMode.Create);
#pragma warning restore SCS0018
        memStream.Position = 0;
        await memStream.CopyToAsync(stream);
        await stream.FlushAsync();

        await SetUnixPermissionsAsync(safePath);

        return (true, null);
    }

    private static bool HasValidMagicBytes(Stream stream, string ext)
    {
        stream.Position = 0;
        var header = new byte[8];
        _ = stream.Read(header, 0, header.Length);
        stream.Position = 0;

        return ext switch
        {
            ".zip" => header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8,
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                              && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
            _ => false
        };
    }

    private static bool IsZipEncrypted(Stream stream)
    {
        stream.Position = 0;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        copy.Position = 0;

        using var zip = new ZipFile(copy);
        foreach (ZipEntry entry in zip)
        {
            if (entry.IsCrypted) return true;
        }
        return false;
    }

    private static async Task SetUnixPermissionsAsync(string filePath)
    {
        var chmod = new ProcessStartInfo
        {
            FileName = "/bin/chmod",
#pragma warning disable SCS0001
            Arguments = $"644 \"{filePath}\"",
#pragma warning restore SCS0001
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(chmod);
        if (process != null) await process.WaitForExitAsync();
    }
}
