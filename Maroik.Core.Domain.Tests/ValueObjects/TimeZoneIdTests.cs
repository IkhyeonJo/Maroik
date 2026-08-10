using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.DomainTests.ValueObjects;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.ValueObjects.TimeZoneId"/>.
/// Covers acceptance of valid IANA zone identifiers, rejection of empty and unrecognised values, and equality.
/// </summary>
public class TimeZoneIdTests
{
    /// <summary>Create returns time zone id, when valid.</summary>
    [Theory]
    [InlineData("UTC")]
    [InlineData("Asia/Seoul")]
    [InlineData("America/New_York")]
    public void Create_ReturnsTimeZoneId_WhenValid(string input)
    {
        var result = TimeZoneId.Create(input);

        Assert.False(result.IsError);
        Assert.Equal(input, result.Value.Value);
    }

    /// <summary>Create returns error, when empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenEmpty(string? input)
    {
        var result = TimeZoneId.Create(input);

        Assert.True(result.IsError);
        Assert.Equal("TimeZoneId.Empty", result.FirstError.Code);
    }

    /// <summary>Create returns error, when unrecognised.</summary>
    [Fact]
    public void Create_ReturnsError_WhenUnrecognised()
    {
        var result = TimeZoneId.Create("Not/AReal/TimeZone");

        Assert.True(result.IsError);
        Assert.Equal("TimeZoneId.Invalid", result.FirstError.Code);
    }

    /// <summary>Equality same value are equal.</summary>
    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = TimeZoneId.Create("UTC").Value;
        var b = TimeZoneId.Create("UTC").Value;

        Assert.Equal(a, b);
    }
}
