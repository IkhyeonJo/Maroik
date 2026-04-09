using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    // ── HashPassword ────────────────────────────────────────────────────────

    [Fact]
    public void HashPassword_ReturnsNonEmptyBcryptHash_ForValidPassword()
    {
        string hash = _sut.HashPassword("MySecurePassword123!");

        Assert.NotNull(hash);
        Assert.StartsWith("$2", hash); // BCrypt hashes start with $2a$ or $2b$
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashEachCall_DueToRandomSalt()
    {
        string hash1 = _sut.HashPassword("SamePassword");
        string hash2 = _sut.HashPassword("SamePassword");

        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HashPassword_ThrowsArgumentException_ForEmptyOrWhitespace(string password)
    {
        Assert.Throws<ArgumentException>(() => _sut.HashPassword(password));
    }

    // ── VerifyPassword ───────────────────────────────────────────────────────

    [Fact]
    public void VerifyPassword_ReturnsTrue_WhenPasswordMatchesStoredHash()
    {
        const string password = "MySecurePassword123!";
        string hash = _sut.HashPassword(password);

        bool result = _sut.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_WhenPasswordDoesNotMatchHash()
    {
        string hash = _sut.HashPassword("CorrectPassword");

        bool result = _sut.VerifyPassword("WrongPassword", hash);

        Assert.False(result);
    }

    [Theory]
    [InlineData("", "$2a$13$validhashplaceholder1234567890")]
    [InlineData("   ", "$2a$13$validhashplaceholder1234567890")]
    public void VerifyPassword_ReturnsFalse_WhenPasswordIsEmptyOrWhitespace(string password, string hash)
    {
        bool result = _sut.VerifyPassword(password, hash);

        Assert.False(result);
    }

    [Theory]
    [InlineData("password", "")]
    [InlineData("password", "   ")]
    public void VerifyPassword_ReturnsFalse_WhenHashIsEmptyOrWhitespace(string password, string hash)
    {
        bool result = _sut.VerifyPassword(password, hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_WhenHashIsMalformed()
    {
        bool result = _sut.VerifyPassword("password", "not-a-valid-bcrypt-hash");

        Assert.False(result);
    }
}
