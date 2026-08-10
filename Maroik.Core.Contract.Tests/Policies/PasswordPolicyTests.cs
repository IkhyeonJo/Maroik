using Maroik.Core.Contract.Misc.Policies;

namespace Maroik.Core.Contract.Tests.Policies;

/// <summary>
/// Unit tests for <see cref="PasswordPolicy"/>.
/// Rule: at least 8 characters, meeting 3 of 4 criteria — uppercase, lowercase, digit, special character.
/// </summary>
public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Abcdefg1")] // upper + lower + digit
    [InlineData("Abcdefg!")] // upper + lower + special
    [InlineData("ABCDEFG1!")] // upper + digit + special
    [InlineData("abcdefg1!")] // lower + digit + special
    [InlineData("Sup3r$ecurePwd")] // all four criteria
    /// <summary>Is valid returns true, when password meets three of four criteria.</summary>
    public void IsValid_ReturnsTrue_WhenPasswordMeetsThreeOfFourCriteria(string password)
    {
        Assert.True(PasswordPolicy.IsValid(password));
    }

    /// <summary>Is valid returns false, when password null or empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValid_ReturnsFalse_WhenPasswordNullOrEmpty(string? password)
    {
        Assert.False(PasswordPolicy.IsValid(password));
    }

    [Theory]
    [InlineData("Abc12")] // meets 3 criteria but under 8 characters
    [InlineData("Abcdefgh")] // only upper + lower (2 criteria)
    [InlineData("abcdefgh1")] // only lower + digit (2 criteria)
    [InlineData("abcdefghijk")] // only lowercase (1 criterion)
    [InlineData("12345678")] // only digits (1 criterion)
    /// <summary>Is valid returns false, when password fails complexity rule.</summary>
    public void IsValid_ReturnsFalse_WhenPasswordFailsComplexityRule(string password)
    {
        Assert.False(PasswordPolicy.IsValid(password));
    }
}
