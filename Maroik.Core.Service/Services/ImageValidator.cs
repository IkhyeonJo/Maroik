using ImageMagick;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <summary>ImageMagick-backed implementation of <see cref="IImageValidator"/>.</summary>
public class ImageValidator : IImageValidator
{
    /// <inheritdoc />
    public bool IsValidImage(byte[] bytes)
    {
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
