using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.DomainTests.AccountDomain;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.Account.Account"/>.
/// Covers creation validation, email confirmation, password reset flow, login attempt tracking,
/// lock/unlock, admin update, soft-delete, profile update, and service-terms acceptance.
/// </summary>
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

    // -- Create ---------------------------------------------------------------

    /// <summary>Create returns account, when all inputs valid.</summary>
    [Fact]
    public void Create_ReturnsAccount_WhenAllInputsValid()
    {
        var result = Account.Create("user@example.com", "hashed", "Nick", "User", "UTC", "KRW", "tok", true);

        Assert.False(result.IsError);
        Assert.Equal("user@example.com", result.Value.Email.Value);
        Assert.Equal("Nick", result.Value.Nickname);
    }

    /// <summary>Create returns error, when email invalid.</summary>
    [Theory]
    [InlineData("notanemail")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ReturnsError_WhenEmailInvalid(string? email)
    {
        var result = Account.Create(email!, "hashed", "Nick", "User", "UTC", null, null, true);

        Assert.True(result.IsError);
    }

    /// <summary>Create returns error, when nickname empty.</summary>
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

    /// <summary>Create returns error, when nickname exceeds 255 characters.</summary>
    [Fact]
    public void Create_ReturnsError_WhenNicknameTooLong()
    {
        var result = Account.Create("user@example.com", "hashed", new string('n', 256), "User", "UTC", null, null, true);

        Assert.True(result.IsError);
        Assert.Equal("Account.NicknameTooLong", result.FirstError.Code);
    }

    /// <summary>Create succeeds when nickname is exactly at the 255-character limit.</summary>
    [Fact]
    public void Create_ReturnsAccount_WhenNicknameAtMaxLength()
    {
        var result = Account.Create("user@example.com", "hashed", new string('n', 255), "User", "UTC", null, null, true);

        Assert.False(result.IsError);
    }

    /// <summary>Create returns error, when password empty.</summary>
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

    /// <summary>Create returns error, when time zone invalid.</summary>
    [Fact]
    public void Create_ReturnsError_WhenTimeZoneInvalid()
    {
        var result = Account.Create("user@example.com", "hashed", "Nick", "User", "Not/Valid", null, null, true);

        Assert.True(result.IsError);
        Assert.Equal("TimeZoneId.Invalid", result.FirstError.Code);
    }

    /// <summary>Create returns error, when role is not Admin or User.</summary>
    [Theory]
    [InlineData("Anonymous")]
    [InlineData("SuperAdmin")]
    [InlineData("")]
    public void Create_ReturnsError_WhenRoleInvalid(string role)
    {
        var result = Account.Create("user@example.com", "hashed", "Nick", role, "UTC", null, null, true);

        Assert.True(result.IsError);
        Assert.Equal("Account.RoleInvalid", result.FirstError.Code);
    }

    // -- ConfirmEmail ---------------------------------------------------------

    /// <summary>Confirm email succeeds, when token matches.</summary>
    [Fact]
    public void ConfirmEmail_Succeeds_WhenTokenMatches()
    {
        var account = ValidAccount(registrationToken: "abc");

        var result = account.ConfirmEmail("abc");

        Assert.False(result.IsError);
        Assert.True(account.EmailConfirmed);
        Assert.Null(account.RegistrationToken);
    }

    /// <summary>Confirm email returns error, when already confirmed.</summary>
    [Fact]
    public void ConfirmEmail_ReturnsError_WhenAlreadyConfirmed()
    {
        var account = ValidAccount(registrationToken: "abc");
        account.ConfirmEmail("abc");

        var result = account.ConfirmEmail("abc");

        Assert.True(result.IsError);
        Assert.Equal("Account.AlreadyConfirmed", result.FirstError.Code);
    }

    /// <summary>Confirm email returns error, when token mismatch.</summary>
    [Fact]
    public void ConfirmEmail_ReturnsError_WhenTokenMismatch()
    {
        var account = ValidAccount(registrationToken: "correct");

        var result = account.ConfirmEmail("wrong");

        Assert.True(result.IsError);
        Assert.Equal("Account.InvalidToken", result.FirstError.Code);
        Assert.False(account.EmailConfirmed);
    }

    // -- RequestPasswordReset -------------------------------------------------

    /// <summary>Request password reset succeeds, when email confirmed.</summary>
    [Fact]
    public void RequestPasswordReset_Succeeds_WhenEmailConfirmed()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ConfirmEmail("tok");

        var result = account.RequestPasswordReset("reset-token");

        Assert.False(result.IsError);
        Assert.Equal("reset-token", account.ResetPasswordToken);
    }

    /// <summary>Request password reset returns error, when email not confirmed.</summary>
    [Fact]
    public void RequestPasswordReset_ReturnsError_WhenEmailNotConfirmed()
    {
        var account = ValidAccount();

        var result = account.RequestPasswordReset("reset-token");

        Assert.True(result.IsError);
        Assert.Equal("Account.NotConfirmed", result.FirstError.Code);
    }

    /// <summary>Request password reset succeeds, when locked, since it is the way a locked account is recovered.</summary>
    [Fact]
    public void RequestPasswordReset_Succeeds_WhenLocked()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ConfirmEmail("tok");
        account.Lock("reason");

        var result = account.RequestPasswordReset("reset-token");

        Assert.False(result.IsError);
        Assert.Equal("reset-token", account.ResetPasswordToken);
    }

    // -- ResetPassword --------------------------------------------------------

    /// <summary>Reset password succeeds, when token valid.</summary>
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

    /// <summary>Reset password returns error, when token invalid.</summary>
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

    /// <summary>Reset password returns error, when new password empty.</summary>
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

    // -- Login attempt tracking ------------------------------------------------

    /// <summary>Record login failure increments counter.</summary>
    [Fact]
    public void RecordLoginFailure_IncrementsCounter()
    {
        var account = ValidAccount();

        account.RecordLoginFailure(maxAttempts: 5);
        account.RecordLoginFailure(maxAttempts: 5);

        Assert.Equal(2, account.LoginAttempt);
        Assert.False(account.Locked);
    }

    /// <summary>Record login failure locks account, when max attempts reached.</summary>
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

    /// <summary>Record login failure locks account, when counter exceeds max.</summary>
    [Fact]
    public void RecordLoginFailure_LocksAccount_WhenCounterExceedsMax()
    {
        var account = ValidAccount();

        account.RecordLoginFailure(maxAttempts: 2);
        account.RecordLoginFailure(maxAttempts: 2);
        account.RecordLoginFailure(maxAttempts: 2);

        Assert.True(account.Locked);
    }

    /// <summary>Reset login attempt sets counter to zero.</summary>
    [Fact]
    public void ResetLoginAttempt_SetsCounterToZero()
    {
        var account = ValidAccount();
        account.RecordLoginFailure(maxAttempts: 5);
        account.RecordLoginFailure(maxAttempts: 5);

        account.ResetLoginAttempt();

        Assert.Equal(0, account.LoginAttempt);
    }

    // -- Lock / Unlock --------------------------------------------------------

    /// <summary>Lock sets locked true and stores message.</summary>
    [Fact]
    public void Lock_SetsLockedTrueAndStoresMessage()
    {
        var account = ValidAccount();

        account.Lock("spam");

        Assert.True(account.Locked);
        Assert.Equal("spam", account.Message);
    }

    /// <summary>Unlock sets locked false and resets attempts.</summary>
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

    // -- AdminUpdate ----------------------------------------------------------

    /// <summary>Admin update returns success, when valid.</summary>
    [Fact]
    public void AdminUpdate_ReturnsSuccess_WhenValid()
    {
        var account = ValidAccount();

        var result = account.AdminUpdate("Admin", "UTC", false, 0, true, true, null, false);

        Assert.False(result.IsError);
        Assert.Equal("Admin", account.Role);
        Assert.True(account.EmailConfirmed);
    }

    /// <summary>Admin update resets login attempt, when unlocked.</summary>
    [Fact]
    public void AdminUpdate_ResetsLoginAttempt_WhenUnlocked()
    {
        var account = ValidAccount();
        account.RecordLoginFailure(maxAttempts: 5);

        var result = account.AdminUpdate(null, "UTC", false, 5, false, true, null, false);

        Assert.False(result.IsError);
        Assert.Equal(0, account.LoginAttempt);
    }

    /// <summary>Admin update returns error, when time zone invalid.</summary>
    [Fact]
    public void AdminUpdate_ReturnsError_WhenTimeZoneInvalid()
    {
        var account = ValidAccount();

        var result = account.AdminUpdate(null, "Not/Valid", false, 0, false, true, null, false);

        Assert.True(result.IsError);
        Assert.Equal("TimeZoneId.Invalid", result.FirstError.Code);
    }

    // -- SoftDelete -----------------------------------------------------------

    /// <summary>Soft delete sets deleted true.</summary>
    [Fact]
    public void SoftDelete_SetsDeletedTrue()
    {
        var account = ValidAccount();

        account.SoftDelete();

        Assert.True(account.Deleted);
    }

    // -- RegenerateRegistrationTokenIfEmpty -----------------------------------

    /// <summary>Regenerate registration token if empty sets token, when current is null.</summary>
    [Fact]
    public void RegenerateRegistrationTokenIfEmpty_SetsToken_WhenCurrentIsNull()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ConfirmEmail("tok");
        Assert.Null(account.RegistrationToken);

        account.RegenerateRegistrationTokenIfEmpty("new-tok");

        Assert.Equal("new-tok", account.RegistrationToken);
    }

    /// <summary>Regenerate registration token if empty does not overwrite, when token exists.</summary>
    [Fact]
    public void RegenerateRegistrationTokenIfEmpty_DoesNotOverwrite_WhenTokenExists()
    {
        var account = ValidAccount(registrationToken: "existing");

        account.RegenerateRegistrationTokenIfEmpty("new-tok");

        Assert.Equal("existing", account.RegistrationToken);
    }

    // -- UpdateProfile --------------------------------------------------------

    /// <summary>Update profile updates all fields.</summary>
    [Fact]
    public void UpdateProfile_UpdatesAllFields()
    {
        var account = ValidAccount();
        var newTz = TimeZoneId.Create("Asia/Seoul").Value;

        account.UpdateProfile("/avatar.png", newTz, "USD");

        Assert.Equal("/avatar.png", account.AvatarImagePath);
        Assert.Equal("Asia/Seoul", account.TimeZone.Value);
        Assert.Equal("USD", account.DefaultMonetaryUnit);
    }

    /// <summary>Update profile clears avatar and currency, when null.</summary>
    [Fact]
    public void UpdateProfile_ClearsAvatarAndCurrency_WhenNull()
    {
        var account = ValidAccount();
        var tz = TimeZoneId.Create("UTC").Value;
        account.UpdateProfile("/avatar.png", tz, "KRW");

        account.UpdateProfile(null, tz, null);

        Assert.Null(account.AvatarImagePath);
        Assert.Null(account.DefaultMonetaryUnit);
    }

    // -- ChangePassword -------------------------------------------------------

    /// <summary>Change password replaces hashed password.</summary>
    [Fact]
    public void ChangePassword_ReplacesHashedPassword()
    {
        var account = ValidAccount();

        account.ChangePassword("newHash");

        Assert.Equal("newHash", account.HashedPassword);
    }

    // -- SetMessage -----------------------------------------------------------

    /// <summary>Set message stores text.</summary>
    [Fact]
    public void SetMessage_StoresText()
    {
        var account = ValidAccount();

        account.SetMessage("Under review");

        Assert.Equal("Under review", account.Message);
    }

    /// <summary>Set message clears message, when null.</summary>
    [Fact]
    public void SetMessage_ClearsMessage_WhenNull()
    {
        var account = ValidAccount();
        account.SetMessage("some message");

        account.SetMessage(null);

        Assert.Null(account.Message);
    }

    // -- ForceConfirmEmail ----------------------------------------------------

    /// <summary>Force confirm email confirms email and clears token.</summary>
    [Fact]
    public void ForceConfirmEmail_ConfirmsEmailAndClearsToken()
    {
        var account = ValidAccount(registrationToken: "tok");

        account.ForceConfirmEmail();

        Assert.True(account.EmailConfirmed);
        Assert.Null(account.RegistrationToken);
    }

    /// <summary>Force confirm email is idempotent, when already confirmed.</summary>
    [Fact]
    public void ForceConfirmEmail_IsIdempotent_WhenAlreadyConfirmed()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ForceConfirmEmail();

        account.ForceConfirmEmail();

        Assert.True(account.EmailConfirmed);
    }

    // -- AcceptServiceTerms ---------------------------------------------------

    /// <summary>Accept service terms sets agreed to true.</summary>
    [Fact]
    public void AcceptServiceTerms_SetsAgreedToTrue()
    {
        var account = ValidAccount(agreedServiceTerms: false);

        account.AcceptServiceTerms();

        Assert.True(account.AgreedServiceTerms);
    }

    // -- SecurityStamp / MustChangePassword ------------------------------------

    /// <summary>A newly created account gets a non-empty security stamp and is not forced to change its password.</summary>
    [Fact]
    public void Create_SetsNonEmptySecurityStamp_AndMustChangePasswordFalse()
    {
        var account = ValidAccount();

        Assert.False(string.IsNullOrEmpty(account.SecurityStamp));
        Assert.False(account.MustChangePassword);
    }

    /// <summary>Self-service change password regenerates the security stamp.</summary>
    [Fact]
    public void ChangePassword_RegeneratesSecurityStamp()
    {
        var account = ValidAccount();
        string originalStamp = account.SecurityStamp;

        account.ChangePassword("newHash");

        Assert.NotEqual(originalStamp, account.SecurityStamp);
    }

    /// <summary>Self-service change password clears a pending forced password change.</summary>
    [Fact]
    public void ChangePassword_ClearsMustChangePassword()
    {
        var account = ValidAccount();
        account.AdminResetPassword("tempHash");

        account.ChangePassword("newHash");

        Assert.False(account.MustChangePassword);
    }

    /// <summary>Admin reset password replaces the hash, forces a password change, and regenerates the security stamp.</summary>
    [Fact]
    public void AdminResetPassword_SetsHashForcesChange_AndRegeneratesSecurityStamp()
    {
        var account = ValidAccount();
        string originalStamp = account.SecurityStamp;

        account.AdminResetPassword("tempHash");

        Assert.Equal("tempHash", account.HashedPassword);
        Assert.True(account.MustChangePassword);
        Assert.NotEqual(originalStamp, account.SecurityStamp);
    }

    /// <summary>Token-based password reset regenerates the security stamp and clears a pending forced password change.</summary>
    [Fact]
    public void ResetPassword_RegeneratesSecurityStamp_AndClearsMustChangePassword()
    {
        var account = ValidAccount(registrationToken: "tok");
        account.ConfirmEmail("tok");
        account.RequestPasswordReset("reset-tok");
        account.AdminResetPassword("tempHash");
        string stampBeforeReset = account.SecurityStamp;

        account.ResetPassword("reset-tok", "newHash");

        Assert.NotEqual(stampBeforeReset, account.SecurityStamp);
        Assert.False(account.MustChangePassword);
    }
}
