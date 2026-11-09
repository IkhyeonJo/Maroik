using Maroik.Core.Domain.Accounts;

namespace Maroik.Core.Domain.Tests.Accounts;

/// <summary>An administrator's save of an account, as one operation of the account.</summary>
public class AccountAdminChangesTests
{
    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime _now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>An unconfirmed user account that has not agreed to the terms.</summary>
    private static Account NewAccount() =>
        Account.Create("user@example.com", "hash", "TestUser", AccountRole.User, "UTC", null, GuidToken.Generate(_now), false, _now).Value;

    /// <summary>A save that changes nothing it may leave alone.</summary>
    private static AdminAccountChanges NoChanges() => new(null, null, null, null, Locked: false, EmailConfirmed: false, AgreedServiceTerms: false, Deleted: false, Message: null);

    /// <summary>Locking an unlocked account ends its sessions; re-saving one that is already locked keeps them.</summary>
    [Fact]
    public void Locking_EndsTheSessions_OnlyWhenTheAccountWasNotLocked()
    {
        Account account = NewAccount();
        string stamp = account.SecurityStamp;

        account.ApplyAdminChanges(NoChanges() with { Locked = true }, _now);
        string lockedStamp = account.SecurityStamp;
        Assert.True(account.Locked);
        Assert.NotEqual(stamp, lockedStamp);

        account.ApplyAdminChanges(NoChanges() with { Locked = true }, _now);
        Assert.Equal(lockedStamp, account.SecurityStamp);
    }

    /// <summary>An unknown role is refused.</summary>
    [Fact]
    public void AnUnknownRole_IsRefused()
    {
        var result = NewAccount().ApplyAdminChanges(NoChanges() with { Role = "Guest" }, _now);

        Assert.True(result.IsError);
    }

    /// <summary>The confirmation and the agreement are one way: a save without the ticks does not take them back.</summary>
    [Fact]
    public void TheConfirmationAndTheAgreement_AreNotTakenBack()
    {
        Account account = NewAccount();
        account.ApplyAdminChanges(NoChanges() with { EmailConfirmed = true, AgreedServiceTerms = true }, _now);

        account.ApplyAdminChanges(NoChanges(), _now);

        Assert.True(account.EmailConfirmed);
        Assert.True(account.AgreedServiceTerms);
    }

    /// <summary>A null value leaves its field as it is; a given one replaces it.</summary>
    [Fact]
    public void ANullValue_LeavesItsFieldAsItIs()
    {
        Account account = NewAccount();
        account.ApplyAdminChanges(NoChanges() with { TimeZoneIanaId = "Asia/Seoul", FileManagerQuotaBytes = 10, FileManagerScanDisabled = true, Role = "Admin", Message = "note" }, _now);

        account.ApplyAdminChanges(NoChanges() with { Message = "note" }, _now);

        Assert.Equal("Asia/Seoul", account.TimeZone.Value);
        Assert.Equal(10, account.FileManagerQuotaBytes);
        Assert.True(account.FileManagerScanDisabled);
        Assert.Equal(AccountRole.Admin, account.Role);
        Assert.Equal("note", account.Message);
    }

    /// <summary>The save decides the deletion both ways.</summary>
    [Fact]
    public void Deleted_SoftDeletesAndRestores()
    {
        Account account = NewAccount();

        account.ApplyAdminChanges(NoChanges() with { Deleted = true }, _now);
        Assert.True(account.Deleted);

        account.ApplyAdminChanges(NoChanges(), _now);
        Assert.False(account.Deleted);
    }
}
