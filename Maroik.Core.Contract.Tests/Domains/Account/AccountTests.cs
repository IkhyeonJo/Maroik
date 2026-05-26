using Maroik.Core.Contract.Domains.Account;

namespace Maroik.Core.Contract.Tests.Domains.AccountDomain;

public class AccountTests
{
    private static Account ValidAccount(
        string email = "user@example.com",
        string hashedPassword = "hashed",
        string nickname = "TestUser",
        string role = "User",
        string timeZone = "UTC",
        string? registrationToken = "token-123",
        bool agreedServiceTerms = true)
        => Account.Create(email, hashedPassword, nickname, role, timeZone, "KRW", registrationToken, agreedServiceTerms).Value;

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ReturnsAccount_WhenAllInputsValid()
    {
        var result = Account.Create("user@example.com", "hashed", "Nick", "User", "UTC", "KRW", "tok", true);

        Assert.False(result.IsError);
        Assert.Equal("user@example.com", result.Value.Email.Value);
        Assert.Equal("Nick", result.Value.Nickname);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ReturnsError_WhenEmailInvalid(string? email)
    {
        var result = Account.Create(email!, "hashed", "Nick", "User", "UTC", null, null, true);

        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ReturnsError_WhenNicknameEmpty(string? nickname)
    {
        var result = Account.Create("user@example.com", "hashed", nickname!, "User", "UTC", null, null, true);

        Assert.True(result.IsError);
        Assert.Equal("Account.NicknameEmpty", result.FirstError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ReturnsError_WhenPasswordEmpty(string? password)
    {
        var result = Account.Create("user@example.com", password!, "Nick", "User", "UTC", null, null, true);

        Assert.True(result.IsError);
        Assert.Equal("Account.PasswordEmpty", result.FirstError.Code);
    }

    [Fact]
    public void Create_ReturnsError_WhenTimeZoneInvalid()
    {
        var result = Account.Create("user@example.com", "hashed", "Nick", "User", "Not/Valid", null, null, true);

        Assert.True(result.IsError);
        Assert.Equal("TimeZoneId.Invalid", result.FirstError.Code);
    }

    // ── ConfirmEmail ─────────────────────────────────────────────────────────

    [Fact]
    public void ConfirmEmail_Succeeds_WhenTokenMatches()
    {
        var account = ValidAccount(registrationToken: "abc");

        var result = account.ConfirmEmail("abc");

        Assert.False(result.IsError);
        Assert.True(account.EmailConfirmed);
        Assert.Null(account.RegistrationToken);
    }

    [Fact]
    public void ConfirmEmail_ReturnsError_WhenAlreadyConfirmed()
    {
        var account = ValidAccount(registrationToken: "abc");
        account.ConfirmEmail("abc");

        var result = account.ConfirmEmail("abc");

        Assert.True(result.IsError);
        Assert.Equal("Account.AlreadyConfirmed", result.FirstError.Code);
    }

    [Fact]
    public void ConfirmEmail_ReturnsError_WhenTokenMismatch()
    {
        var account = ValidAccount(registrationToken: "correct");

        var result = account.ConfirmEmail("wrong");

        Assert.True(result.IsError);
        Assert.Equal("Account.InvalidToken", result.FirstError.Code);
        Assert.False(account.EmailConfirmed);
    }

    // ── RequestPasswordReset ─────────────────────────────────────────────────

    [Fact]
    public void RequestPasswordReset_Succeeds_WhenEmailConfirmed()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ConfirmEmail("tok");

        var result = account.RequestPasswordReset("reset-token");

        Assert.False(result.IsError);
        Assert.Equal("reset-token", account.ResetPasswordToken);
    }

    [Fact]
    public void RequestPasswordReset_ReturnsError_WhenEmailNotConfirmed()
    {
        var account = ValidAccount();

        var result = account.RequestPasswordReset("reset-token");

        Assert.True(result.IsError);
        Assert.Equal("Account.NotConfirmed", result.FirstError.Code);
    }

    [Fact]
    public void RequestPasswordReset_ReturnsError_WhenLocked()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ConfirmEmail("tok");
        account.Lock("reason");

        var result = account.RequestPasswordReset("reset-token");

        Assert.True(result.IsError);
        Assert.Equal("Account.Locked", result.FirstError.Code);
    }

    // ── ResetPassword ────────────────────────────────────────────────────────

    [Fact]
    public void ResetPassword_Succeeds_WhenTokenValid()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ConfirmEmail("tok");
        account.RequestPasswordReset("reset-tok");

        var result = account.ResetPassword("reset-tok", "newHash");

        Assert.False(result.IsError);
        Assert.Equal("newHash", account.HashedPassword);
        Assert.Null(account.ResetPasswordToken);
    }

    [Fact]
    public void ResetPassword_ReturnsError_WhenTokenInvalid()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ConfirmEmail("tok");
        account.RequestPasswordReset("reset-tok");

        var result = account.ResetPassword("wrong-tok", "newHash");

        Assert.True(result.IsError);
        Assert.Equal("Account.InvalidToken", result.FirstError.Code);
    }

    [Fact]
    public void ResetPassword_ReturnsError_WhenNewPasswordEmpty()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ConfirmEmail("tok");
        account.RequestPasswordReset("reset-tok");

        var result = account.ResetPassword("reset-tok", "");

        Assert.True(result.IsError);
        Assert.Equal("Account.PasswordEmpty", result.FirstError.Code);
    }

    // ── Login attempt tracking ────────────────────────────────────────────────

    [Fact]
    public void RecordLoginFailure_IncrementsCounter()
    {
        var account = ValidAccount();

        account.RecordLoginFailure();
        account.RecordLoginFailure();

        Assert.Equal(2, account.LoginAttempt);
    }

    [Fact]
    public void ResetLoginAttempt_SetsCounterToZero()
    {
        var account = ValidAccount();
        account.RecordLoginFailure();
        account.RecordLoginFailure();

        account.ResetLoginAttempt();

        Assert.Equal(0, account.LoginAttempt);
    }

    // ── Lock / Unlock ────────────────────────────────────────────────────────

    [Fact]
    public void Lock_SetsLockedTrueAndStoresMessage()
    {
        var account = ValidAccount();

        account.Lock("spam");

        Assert.True(account.Locked);
        Assert.Equal("spam", account.Message);
    }

    [Fact]
    public void Unlock_SetsLockedFalseAndResetsAttempts()
    {
        var account = ValidAccount();
        account.RecordLoginFailure();
        account.Lock();

        account.Unlock();

        Assert.False(account.Locked);
        Assert.Equal(0, account.LoginAttempt);
    }

    // ── AdminUpdate ──────────────────────────────────────────────────────────

    [Fact]
    public void AdminUpdate_ReturnsSuccess_WhenValid()
    {
        var account = ValidAccount();

        var result = account.AdminUpdate("Admin", "UTC", false, 0, true, true, null, false);

        Assert.False(result.IsError);
        Assert.Equal("Admin", account.Role);
        Assert.True(account.EmailConfirmed);
    }

    [Fact]
    public void AdminUpdate_ResetsLoginAttempt_WhenUnlocked()
    {
        var account = ValidAccount();
        account.RecordLoginFailure();

        var result = account.AdminUpdate(null, "UTC", false, 5, false, true, null, false);

        Assert.False(result.IsError);
        Assert.Equal(0, account.LoginAttempt);
    }

    [Fact]
    public void AdminUpdate_ReturnsError_WhenTimeZoneInvalid()
    {
        var account = ValidAccount();

        var result = account.AdminUpdate(null, "Not/Valid", false, 0, false, true, null, false);

        Assert.True(result.IsError);
        Assert.Equal("TimeZoneId.Invalid", result.FirstError.Code);
    }

    // ── SoftDelete ───────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_SetsDeletedTrue()
    {
        var account = ValidAccount();

        account.SoftDelete();

        Assert.True(account.Deleted);
    }

    // ── RegenerateRegistrationTokenIfEmpty ───────────────────────────────────

    [Fact]
    public void RegenerateRegistrationTokenIfEmpty_SetsToken_WhenCurrentIsNull()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ConfirmEmail("tok");
        Assert.Null(account.RegistrationToken);

        account.RegenerateRegistrationTokenIfEmpty("new-tok");

        Assert.Equal("new-tok", account.RegistrationToken);
    }

    [Fact]
    public void RegenerateRegistrationTokenIfEmpty_DoesNotOverwrite_WhenTokenExists()
    {
        var account = ValidAccount(registrationToken: "existing");

        account.RegenerateRegistrationTokenIfEmpty("new-tok");

        Assert.Equal("existing", account.RegistrationToken);
    }
}
