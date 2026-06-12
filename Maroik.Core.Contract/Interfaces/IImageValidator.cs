namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Validates raw image bytes without coupling callers to a specific imaging library.
/// </summary>
public interface IImageValidator
{
    /// <summary>Returns true when <paramref name="bytes"/> can be decoded as a valid image.</summary>
    bool IsValidImage(byte[] bytes);

    /// <summary>Returns true when <paramref name="bytes"/> represent an SVG image.</summary>
    bool IsSvg(byte[] bytes);
}
