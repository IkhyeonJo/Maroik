using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Tests.Domains.ValueObjects;

public class TimeZoneIdTests
{
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

    [Fact]
    public void Create_ReturnsError_WhenUnrecognised()
    {
        var result = TimeZoneId.Create("Not/AReal/TimeZone");

        Assert.True(result.IsError);
        Assert.Equal("TimeZoneId.Invalid", result.FirstError.Code);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = TimeZoneId.Create("UTC").Value;
        var b = TimeZoneId.Create("UTC").Value;

        Assert.Equal(a, b);
    }
}
