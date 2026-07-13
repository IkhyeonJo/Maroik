using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>Unit tests for <see cref="ImageValidatorService"/>.</summary>
public class ImageValidatorServiceTests
{
    private readonly ImageValidatorService _sut = new();

    // Minimal valid 1x1 transparent PNG — a well-known fixture byte sequence, not a mock.
    private static readonly byte[] ValidPngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89,
        0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54,
        0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01,
        0x0D, 0x0A, 0x2D, 0xB4,
        0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
    ];

    /// <summary>Verifies that a real, decodable PNG is accepted.</summary>
    [Fact]
    public void IsValidImage_ReturnsTrue_ForValidPng()
    {
        Assert.True(_sut.IsValidImage(ValidPngBytes));
    }

    /// <summary>Verifies that non-image bytes are rejected rather than throwing.</summary>
    [Fact]
    public void IsValidImage_ReturnsFalse_ForGarbageBytes()
    {
        Assert.False(_sut.IsValidImage([0x00, 0x01, 0x02, 0x03]));
    }

    /// <summary>Verifies that empty input is rejected rather than throwing.</summary>
    [Fact]
    public void IsValidImage_ReturnsFalse_ForEmptyBytes()
    {
        Assert.False(_sut.IsValidImage([]));
    }

    /// <summary>Verifies that a non-SVG (PNG) image is not reported as SVG.</summary>
    [Fact]
    public void IsSvg_ReturnsFalse_ForPngImage()
    {
        Assert.False(_sut.IsSvg(ValidPngBytes));
    }

    /// <summary>Verifies that garbage bytes are rejected rather than throwing.</summary>
    [Fact]
    public void IsSvg_ReturnsFalse_ForGarbageBytes()
    {
        Assert.False(_sut.IsSvg([0x00, 0x01, 0x02, 0x03]));
    }
}
