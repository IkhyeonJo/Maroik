using Maroik.Core.Contract.Dtos;

namespace Maroik.Website.Extensions;

/// <summary>
/// Extension methods for <see cref="IFormFile"/> to simplify reading uploaded files
/// into service-layer data transfer objects.
/// </summary>
public static class FormFileExtensions
{
    /// <summary>
    /// Reads the uploaded file into an <see cref="AttachedFileInfo"/>.
    /// Returns <see langword="null"/> when <paramref name="file"/> is <see langword="null"/>.
    /// </summary>
    public static async Task<AttachedFileInfo?> ToAttachedFileInfoAsync(this IFormFile? file, CancellationToken ct = default)
    {
        if (file == null) return null;
        using MemoryStream ms = new();
        await file.CopyToAsync(ms, ct);
        return new AttachedFileInfo
        {
            Bytes = ms.ToArray(),
            ContentType = file.ContentType,
            FileName = file.FileName,
            Size = file.Length
        };
    }
}
