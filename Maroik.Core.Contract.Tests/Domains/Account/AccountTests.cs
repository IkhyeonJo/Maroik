using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Domains.ValueObjects;
using Maroik.Core.Contract.Utilities;

namespace Maroik.Core.Contract.Tests.Domains.AccountDomain;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Contract.Domains.Account.Account"/>.
/// Covers creation validation, email confirmation, password reset flow, login attempt tracking,
/// lock/unlock, admin update, soft-delete, profile update, and service-terms acceptance.
/// </summary>
public class AccountTests
{
    private static Account ValidAccount(
        string email = "user@example.com",
        string hashedPassword = "hashed",
        string nickname = "TestUser",
        string role = Role.User,
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

        account.RecordLoginFailure(maxAttempts: 5);
        account.RecordLoginFailure(maxAttempts: 5);

        Assert.Equal(2, account.LoginAttempt);
        Assert.False(account.Locked);
    }

    [Fact]
    public void RecordLoginFailure_LocksAccount_WhenMaxAttemptsReached()
    {
        var account = ValidAccount();

        account.RecordLoginFailure(maxAttempts: 3);
        account.RecordLoginFailure(maxAttempts: 3);
        account.RecordLoginFailure(maxAttempts: 3);

        Assert.Equal(3, account.LoginAttempt);
        Assert.True(account.Locked);
    }

    [Fact]
    public void RecordLoginFailure_LocksAccount_WhenCounterExceedsMax()
    {
        var account = ValidAccount();

        account.RecordLoginFailure(maxAttempts: 2);
        account.RecordLoginFailure(maxAttempts: 2);
        account.RecordLoginFailure(maxAttempts: 2);

        Assert.True(account.Locked);
    }

    [Fact]
    public void ResetLoginAttempt_SetsCounterToZero()
    {
        var account = ValidAccount();
        account.RecordLoginFailure(maxAttempts: 5);
        account.RecordLoginFailure(maxAttempts: 5);

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
        account.RecordLoginFailure(maxAttempts: 5);
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

        var result = account.AdminUpdate(Role.Admin, "UTC", false, 0, true, true, null, false);

        Assert.False(result.IsError);
        Assert.Equal(Role.Admin, account.Role);
        Assert.True(account.EmailConfirmed);
    }

    [Fact]
    public void AdminUpdate_ResetsLoginAttempt_WhenUnlocked()
    {
        var account = ValidAccount();
        account.RecordLoginFailure(maxAttempts: 5);

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

    // ── UpdateProfile ────────────────────────────────────────────────────────

    [Fact]
    public void UpdateProfile_UpdatesAllFields()
    {
        var account = ValidAccount();
        var newTz = TimeZoneId.Create("Asia/Seoul").Value;

        account.UpdateProfile("NewNick", "/avatar.png", newTz, "USD");

        Assert.Equal("NewNick", account.Nickname);
        Assert.Equal("/avatar.png", account.AvatarImagePath);
        Assert.Equal("Asia/Seoul", account.TimeZone.Value);
        Assert.Equal("USD", account.DefaultMonetaryUnit);
    }

    [Fact]
    public void UpdateProfile_ClearsAvatarAndCurrency_WhenNull()
    {
        var account = ValidAccount();
        var tz = TimeZoneId.Create("UTC").Value;
        account.UpdateProfile("Nick", "/avatar.png", tz, "KRW");

        account.UpdateProfile("Nick", null, tz, null);

        Assert.Null(account.AvatarImagePath);
        Assert.Null(account.DefaultMonetaryUnit);
    }

    // ── ChangePassword ───────────────────────────────────────────────────────

    [Fact]
    public void ChangePassword_ReplacesHashedPassword()
    {
        var account = ValidAccount();

        account.ChangePassword("newHash");

        Assert.Equal("newHash", account.HashedPassword);
    }

    // ── SetMessage ───────────────────────────────────────────────────────────

    [Fact]
    public void SetMessage_StoresText()
    {
        var account = ValidAccount();

        account.SetMessage("Under review");

        Assert.Equal("Under review", account.Message);
    }

    [Fact]
    public void SetMessage_ClearsMessage_WhenNull()
    {
        var account = ValidAccount();
        account.SetMessage("some message");

        account.SetMessage(null);

        Assert.Null(account.Message);
    }

    // ── ForceConfirmEmail ────────────────────────────────────────────────────

    [Fact]
    public void ForceConfirmEmail_ConfirmsEmailAndClearsToken()
    {
        var account = ValidAccount(registrationToken: "tok");

        account.ForceConfirmEmail();

        Assert.True(account.EmailConfirmed);
        Assert.Null(account.RegistrationToken);
    }

    [Fact]
    public void ForceConfirmEmail_IsIdempotent_WhenAlreadyConfirmed()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ForceConfirmEmail();

        account.ForceConfirmEmail();

        Assert.True(account.EmailConfirmed);
    }

    // ── AcceptServiceTerms ───────────────────────────────────────────────────

    [Fact]
    public void AcceptServiceTerms_SetsAgreedToTrue()
    {
        var account = ValidAccount(agreedServiceTerms: false);

        account.AcceptServiceTerms();

        Assert.True(account.AgreedServiceTerms);
    }
}
