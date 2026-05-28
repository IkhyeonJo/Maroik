using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="PasswordService"/>.
/// Verifies BCrypt hash generation (work factor 13), hash/plain-text non-equality,
/// and successful verification of a hashed password against its plain-text original.
/// </summary>
public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    // -- HashPassword --------------------------------------------------------

    /// <summary>Verifies that <c>HashPassword</c> returns non empty bcrypt hash for valid password.</summary>
    [Fact]
    public void HashPassword_ReturnsNonEmptyBcryptHash_ForValidPassword()
    {
        string hash = _sut.HashPassword("MySecurePassword123!");

        Assert.NotNull(hash);
        Assert.StartsWith("$2", hash); // BCrypt hashes start with $2a$ or $2b$
    }

    /// <summary>Verifies that <c>HashPassword</c> produces different hash each call when due to random salt.</summary>
    [Fact]
    public void HashPassword_ProducesDifferentHashEachCall_DueToRandomSalt()
    {
        string hash1 = _sut.HashPassword("SamePassword");
        string hash2 = _sut.HashPassword("SamePassword");

        Assert.NotEqual(hash1, hash2);
    }

    /// <summary>Verifies that <c>HashPassword</c> throws argument exception for empty or whitespace.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HashPassword_ThrowsArgumentException_ForEmptyOrWhitespace(string password)
    {
        Assert.Throws<ArgumentException>(() => _sut.HashPassword(password));
    }

    // -- VerifyPassword -------------------------------------------------------

    /// <summary>Verifies that <c>VerifyPassword</c> returns true when password matches stored hash.</summary>
    [Fact]
    public void VerifyPassword_ReturnsTrue_WhenPasswordMatchesStoredHash()
    {
        const string password = "MySecurePassword123!";
        string hash = _sut.HashPassword(password);

        bool result = _sut.VerifyPassword(password, hash);

        Assert.True(result);
    }

    /// <summary>Verifies that <c>VerifyPassword</c> returns false when password does not match hash.</summary>
    [Fact]
    public void VerifyPassword_ReturnsFalse_WhenPasswordDoesNotMatchHash()
    {
        string hash = _sut.HashPassword("CorrectPassword");

        bool result = _sut.VerifyPassword("WrongPassword", hash);

        Assert.False(result);
    }

    /// <summary>Verifies that <c>VerifyPassword</c> returns false when password is empty or whitespace.</summary>
    [Theory]
    [InlineData("", "$2a$13$validhashplaceholder1234567890")]
    [InlineData("   ", "$2a$13$validhashplaceholder1234567890")]
    public void VerifyPassword_ReturnsFalse_WhenPasswordIsEmptyOrWhitespace(string password, string hash)
    {
        bool result = _sut.VerifyPassword(password, hash);

        Assert.False(result);
    }

    /// <summary>Verifies that <c>VerifyPassword</c> returns false when hash is empty or whitespace.</summary>
    [Theory]
    [InlineData("password", "")]
    [InlineData("password", "   ")]
    public void VerifyPassword_ReturnsFalse_WhenHashIsEmptyOrWhitespace(string password, string hash)
    {
        bool result = _sut.VerifyPassword(password, hash);

        Assert.False(result);
    }

    /// <summary>Verifies that <c>VerifyPassword</c> returns false when hash is malformed.</summary>
    [Fact]
    public void VerifyPassword_ReturnsFalse_WhenHashIsMalformed()
    {
        bool result = _sut.VerifyPassword("password", "not-a-valid-bcrypt-hash");

        Assert.False(result);
    }
}
