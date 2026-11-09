using Maroik.Core.Domain.Accounts;

namespace Maroik.Core.Domain.Tests.Accounts;

/// <summary>The password-reset and self-registration rules the account answers itself, not its callers.</summary>
public class AccountPasswordResetRuleTests
{
    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime _now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A confirmed account with a pending reset of <paramref name="resetToken"/>.</summary>
    private static Account ConfirmedAccount(string resetToken)
    {
        string registrationToken = GuidToken.Generate(_now);
        Account account = Account.Create("user@example.com", "hash", "TestUser", AccountRole.User, "UTC", null, registrationToken, true, _now).Value;
        account.ConfirmEmail(registrationToken, _now);
        account.RequestPasswordReset(resetToken, _now);
        return account;
    }

    /// <summary>A deleted account gets no reset token.</summary>
    [Fact]
    public void RequestPasswordReset_IsRefused_ForADeletedAccount()
    {
        Account account = ConfirmedAccount(GuidToken.Generate(_now));
        account.SoftDelete(_now);

        Assert.Equal("Account.Deleted", account.RequestPasswordReset(GuidToken.Generate(_now), _now).FirstError.Code);
    }

    /// <summary>A link mailed before the deletion no longer resets the password.</summary>
    [Fact]
    public void ResetPassword_IsRefused_ForADeletedAccount()
    {
        string token = GuidToken.Generate(_now);
        Account account = ConfirmedAccount(token);
        account.SoftDelete(_now);

        var result = account.ResetPassword(token, "new-hash", _now);

        Assert.Equal("Account.InvalidToken", result.FirstError.Code);
        Assert.Equal("hash", account.HashedPassword);
    }

    /// <summary>A successful reset always unlocks the account and clears the failed-login counter, an administrator's lock included.</summary>
    [Fact]
    public void ResetPassword_UnlocksTheAccount()
    {
        string token = GuidToken.Generate(_now);
        Account account = ConfirmedAccount(token);
        account.RecordLoginFailure(1, _now);
        Assert.True(account.Locked);

        Assert.False(account.ResetPassword(token, "new-hash", _now).IsError);

        Assert.False(account.Locked);
        Assert.Equal(0, account.LoginAttempt);
    }

    /// <summary>A failed reset leaves the lock in place.</summary>
    [Fact]
    public void ResetPassword_KeepsTheLock_WhenItFails()
    {
        Account account = ConfirmedAccount(GuidToken.Generate(_now));
        account.RecordLoginFailure(1, _now);

        Assert.True(account.ResetPassword("wrong-token", "new-hash", _now).IsError);

        Assert.True(account.Locked);
    }

    /// <summary>Nobody registers without accepting the service terms.</summary>
    [Fact]
    public void RequireServiceTermsForRegistration_RefusesARegistrationWithoutConsent()
    {
        Assert.Equal("Account.ServiceTermsRequired", Account.RequireServiceTermsForRegistration(false).FirstError.Code);
        Assert.False(Account.RequireServiceTermsForRegistration(true).IsError);
    }
}
