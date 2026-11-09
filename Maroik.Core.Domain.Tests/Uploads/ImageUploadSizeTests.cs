using Maroik.Core.Domain.Uploads;

namespace Maroik.Core.Domain.Tests.Uploads;

/// <summary>An uploaded image must hold something and stay within the configured limit.</summary>
public class ImageUploadSizeTests
{
    /// <summary>An empty image and one past the limit are refused; the limit itself is accepted.</summary>
    [Theory]
    [InlineData(0, 10, false)]
    [InlineData(-1, 10, false)]
    [InlineData(1, 10, true)]
    [InlineData(10, 10, true)]
    [InlineData(11, 10, false)]
    public void IsValidSize_IsNonEmptyAndWithinTheLimit(long size, long max, bool expected)
    {
        Assert.Equal(expected, ImageUploadPolicy.IsValidSize(size, max));
    }
}
