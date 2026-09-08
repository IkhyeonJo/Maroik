using ImageMagick;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <summary>ImageMagick-backed implementation of <see cref="IImageValidatorService"/>.</summary>
public class ImageValidatorService : IImageValidatorService
{
    static ImageValidatorService()
    {
        // These are process-global (Magick.NET has no per-instance limits), and this service is the
        // only Magick.NET consumer in the process. Bound decode work so a small "pixel bomb" — a
        // tiny file that declares enormous dimensions — can't exhaust memory. The caps sit far above
        // any real avatar or Summernote image.
        ResourceLimits.Width = 20_000;
        ResourceLimits.Height = 20_000;
        ResourceLimits.Area = 100_000_000;          // ~100 MP
        ResourceLimits.Memory = 256UL * 1024 * 1024; // 256 MiB
    }

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
