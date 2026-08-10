using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.DomainTests.ValueObjects;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.ValueObjects.Email"/>.
/// Covers creation with valid inputs, empty/invalid format rejections, lower-case normalisation, and equality.
/// </summary>
public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("user.name+tag@sub.domain.org")]
    public void Create_ReturnsEmail_WhenValid(string input)
    {
        var result = Email.Create(input);

        Assert.False(result.IsError);
        Assert.Equal(input.ToLowerInvariant(), result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenEmpty(string? input)
    {
        var result = Email.Create(input);

        Assert.True(result.IsError);
        Assert.Equal("Email.Empty", result.FirstError.Code);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@nodomain")]
    [InlineData("noatsign.com")]
    [InlineData("missing@tld")]
    public void Create_ReturnsError_WhenInvalidFormat(string input)
    {
        var result = Email.Create(input);

        Assert.True(result.IsError);
        Assert.Equal("Email.Invalid", result.FirstError.Code);
    }

    [Fact]
    public void Create_NormalizesToLowerCase()
    {
        var result = Email.Create("User@Example.COM");

        Assert.False(result.IsError);
        Assert.Equal("user@example.com", result.Value.Value);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = Email.Create("user@example.com").Value;
        var b = Email.Create("user@example.com").Value;

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = Email.Create("a@example.com").Value;
        var b = Email.Create("b@example.com").Value;

        Assert.NotEqual(a, b);
    }
}
