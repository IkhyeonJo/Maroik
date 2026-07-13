using ImageMagick;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <summary>ImageMagick-backed implementation of <see cref="IImageValidatorService"/>.</summary>
public class ImageValidatorService : IImageValidatorService
{
    /// <inheritdoc />
    public bool IsValidImage(byte[] bytes)
    {
        // MagickImage's constructor throws ArgumentException (not MagickException) for an empty
        // stream, so this must be rejected before the decode attempt rather than via the catch below.
        if (bytes is not { Length: > 0 })
            return false;

        try
        {
            using var image = new MagickImage(new MemoryStream(bytes));
            return true;
        }
        catch (MagickException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsSvg(byte[] bytes)
    {
        if (bytes is not { Length: > 0 })
            return false;

        try
        {
            using var image = new MagickImage(new MemoryStream(bytes));
            return image.Format == MagickFormat.Svg;
        }
        catch (MagickException)
        {
            return false;
        }
    }
}
