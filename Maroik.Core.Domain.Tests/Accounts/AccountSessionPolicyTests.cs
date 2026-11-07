using Maroik.Core.Domain.Accounts;

namespace Maroik.Core.Domain.Tests.Accounts;

/// <summary>Unit tests for <see cref="AccountSessionPolicy"/>: which account states end a signed-in session, and the reason logged.</summary>
public class AccountSessionPolicyTests
{
    /// <summary>A session whose stamp still matches its live account stands.</summary>
    [Fact]
    public void InvalidationReason_IsNull_WhenTheAccountIsLiveAndTheStampMatches() =>
        Assert.Null(AccountSessionPolicy.InvalidationReason(accountExists: true, deleted: false, currentStamp: "stamp", sessionStamp: "stamp"));

    /// <summary>A session whose account has no row any more ends.</summary>
    [Fact]
    public void InvalidationReason_NamesAMissingAccount() =>
        Assert.Equal(SessionInvalidationReason.AccountMissing, AccountSessionPolicy.InvalidationReason(accountExists: false, deleted: false, currentStamp: null, sessionStamp: "stamp"));

    /// <summary>A session of a soft-deleted account ends, even when the stamp still matches.</summary>
    [Fact]
    public void InvalidationReason_NamesADeletedAccount() =>
        Assert.Equal(SessionInvalidationReason.AccountDeleted, AccountSessionPolicy.InvalidationReason(accountExists: true, deleted: true, currentStamp: "stamp", sessionStamp: "stamp"));

    /// <summary>A stamp that moved on (password change, admin lock) ends the session.</summary>
    [Fact]
    public void InvalidationReason_NamesAChangedStamp() =>
        Assert.Equal(SessionInvalidationReason.SecurityStampChanged, AccountSessionPolicy.InvalidationReason(accountExists: true, deleted: false, currentStamp: "new", sessionStamp: "old"));

    /// <summary>The stamp is compared exactly: one differing only in letter case is another stamp.</summary>
    [Fact]
    public void InvalidationReason_ComparesTheStampExactly() =>
        Assert.Equal(SessionInvalidationReason.SecurityStampChanged, AccountSessionPolicy.InvalidationReason(accountExists: true, deleted: false, currentStamp: "STAMP", sessionStamp: "stamp"));
}
