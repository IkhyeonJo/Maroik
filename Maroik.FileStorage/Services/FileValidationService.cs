using ICSharpCode.SharpZipLib.Zip;
using Maroik.Core.Contract.Interfaces;
using Maroik.FileStorage.Contracts;
using Maroik.FileStorage.Helpers;
using Maroik.FileStorage.Settings;
using Microsoft.Extensions.Options;

namespace Maroik.FileStorage.Services;

/// <summary>
/// Validates uploaded files against extension, size, magic-byte, ZIP-encryption,
/// and ClamAV virus-scan rules before writing them to disk.
/// Extracted from <c>FileController</c> to keep the controller free of infrastructure concerns.
/// </summary>
public class FileValidationService(IClamavClient clamavClient, IOptions<FileStorageSetting> settings) : IFileValidationService
{
    private static readonly string[] _allowedExtensions = [".zip", ".jpg", ".jpeg", ".png"];

    /// <inheritdoc />
    public async Task<(bool Success, string? Error)> ValidateAndSaveAsync(IFormFile file, string filePath)
    {
        if (file is not { Length: > 0 })
            return (false, "Invalid file.");

        var fileName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        // Confine the target directory to the storage root. The callers only ever pass
        // server-generated relative paths under "upload/"; anything that resolves outside the root
        // (via "..", an absolute path, a symlink, …) is rejected here rather than trusted.
        var safeDir = StoragePath.ResolveWithinRoot(settings.Value.StorageRootPath, filePath);
        if (safeDir == null)
            return (false, "Invalid file path.");
        var safePath = Path.Combine(safeDir, fileName);

        if (!_allowedExtensions.Contains(ext))
            return (false, "Unsupported file extension.");

        if (file.Length > FileStorageSetting.MaxAttachedFileSizeBytes)
            return (false, "File too large.");

        using var memStream = new MemoryStream();
        await file.CopyToAsync(memStream);

        if (!HasValidMagicBytes(memStream, ext))
            return (false, "Invalid or unsupported file format.");

        if (!await clamavClient.ScanWithClamavAsync(memStream, settings.Value.ClamavHost ?? "", settings.Value.ClamavPort))
            return (false, "File may be infected with a virus.");

        if (ext == ".zip" && IsZipEncrypted(memStream))
            return (false, "Encrypted ZIP files are not allowed.");

        if (!Directory.Exists(safeDir))
            Directory.CreateDirectory(safeDir);

        // Every caller generates a fresh GUID-based file name per upload (see BoardService/
        // CalendarService/AttachmentContentService), so a path collision here means something
        // is wrong upstream — fail loudly instead of silently overwriting existing data.
        if (File.Exists(safePath))
            return (false, "A file already exists at the target path.");

        // safePath is built from Path.GetFileName(file.FileName) combined into safeDir above,
        // so it cannot escape the target directory; suppress the path-traversal false positive.
#pragma warning disable SCS0018
        await using var stream = new FileStream(safePath, FileMode.CreateNew);
#pragma warning restore SCS0018
        memStream.Position = 0;
        await memStream.CopyToAsync(stream);
        await stream.FlushAsync();

        SetReadableFileMode(safePath);

        return (true, null);
    }

    /// <summary>
    /// Sets mode 644 on the newly written file so the process serving downloads can read it
    /// regardless of the container's umask. Uses the framework API rather than shelling out to
    /// <c>/bin/chmod</c>; a no-op on Windows (dev only — the deployment target is Linux).
    /// </summary>
    private static void SetReadableFileMode(string filePath)
    {
        if (OperatingSystem.IsWindows())
            return;

        File.SetUnixFileMode(
            filePath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite |
            UnixFileMode.GroupRead | UnixFileMode.OtherRead);
    }

    /// <summary>
    /// Checks the leading bytes of <paramref name="stream"/> against the known signature for
    /// <paramref name="ext"/>, so a file cannot bypass validation merely by having a permitted extension
    /// (e.g. an executable renamed to <c>.png</c>).
    /// </summary>
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

    /// <summary>
    /// Returns <see langword="true"/> if any entry in the ZIP archive is password-protected.
    /// Encrypted entries cannot be scanned by ClamAV, so such archives are rejected outright.
    /// </summary>
    private static bool IsZipEncrypted(Stream stream)
    {
        stream.Position = 0;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        copy.Position = 0;

        using var zip = new ZipFile(copy);
        return zip.Cast<ZipEntry>().Any(entry => entry.IsCrypted);
    }
}
