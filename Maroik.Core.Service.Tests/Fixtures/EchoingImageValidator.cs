using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>
/// An <see cref="IImageValidatorService"/> fake whose <c>StripMetadata</c> hands the bytes back unchanged, labelled
/// the way the real validator labels them — PNG when they start with the PNG signature, JPEG otherwise — so a test
/// that is not about re-encoding still gets a usable result. A test may override it with its own setup.
/// </summary>
internal static class EchoingImageValidator
{
    /// <summary>The 8-byte signature every PNG file starts with.</summary>
    private static readonly byte[] _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>A fake with the echoing <c>StripMetadata</c> described on the class.</summary>
    public static FakeImageValidatorService Create()
    {
        var fake = new FakeImageValidatorService();
        fake.AnswerStripMetadata(bytes => bytes.AsSpan().StartsWith(_pngSignature)
                ? new StrippedImage(bytes, ".png", "image/png")
                : new StrippedImage(bytes, ".jpg", "image/jpeg"));
        return fake;
    }
}
