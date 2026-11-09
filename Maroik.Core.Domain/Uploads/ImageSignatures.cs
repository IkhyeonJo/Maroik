namespace Maroik.Core.Domain.Uploads;

/// <summary>The leading bytes that prove an image's format, whatever its name or claimed type says.</summary>
public static class ImageSignatures
{
    /// <summary>Whether <paramref name="bytes"/> start with the 8-byte signature of every PNG file.</summary>
    public static bool IsPng(ReadOnlySpan<byte> bytes) => bytes.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

    /// <summary>Whether <paramref name="bytes"/> start with the SOI marker plus the first byte of the next marker, as every JPEG file does.</summary>
    public static bool IsJpeg(ReadOnlySpan<byte> bytes) => bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]);
}
