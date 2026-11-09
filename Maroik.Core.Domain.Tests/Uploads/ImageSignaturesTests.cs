using Maroik.Core.Domain.Uploads;

namespace Maroik.Core.Domain.Tests.Uploads;

/// <summary>The leading bytes that prove an image's format.</summary>
public class ImageSignaturesTests
{
    /// <summary>A PNG is proven by all eight bytes of its signature, not by the first few.</summary>
    [Fact]
    public void IsPng_NeedsTheWholeSignature()
    {
        Assert.True(ImageSignatures.IsPng([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]));
        Assert.False(ImageSignatures.IsPng([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A]));
        Assert.False(ImageSignatures.IsPng([0xFF, 0xD8, 0xFF]));
    }

    /// <summary>A JPEG is proven by its three leading bytes.</summary>
    [Fact]
    public void IsJpeg_NeedsThreeBytes()
    {
        Assert.True(ImageSignatures.IsJpeg([0xFF, 0xD8, 0xFF, 0xE0]));
        Assert.False(ImageSignatures.IsJpeg([0xFF, 0xD8]));
        Assert.False(ImageSignatures.IsJpeg([]));
    }
}
