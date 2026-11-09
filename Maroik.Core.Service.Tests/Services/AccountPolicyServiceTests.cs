using Maroik.Core.Contract.Dtos;
using Maroik.Core.Service.Services;
using Maroik.Core.Domain.Accounts;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AccountPolicyService"/> — the owner / owner-or-admin
/// authorization rules used to gate edit/delete UI affordances for board posts, comments,
/// and private notes.
/// </summary>
public class AccountPolicyServiceTests
{
    /// <summary>The service under test.</summary>
    private readonly AccountPolicyService _sut = new();

    // -- IsOwner ----------------------------------------------------------------

    // -- IsOwnerOrAdmin -----------------------------------------------------------

    // -- SeesLocalTime ------------------------------------------------------------

    /// <summary>An Admin viewer sees local time.</summary>
    [Fact]
    public void SeesLocalTime_Admin_True()
    {
        var viewer = new AccountResponse { Nickname = "AdminUser", Role = Role.Admin };
        Assert.True(_sut.SeesLocalTime(viewer));
    }

    /// <summary>A regular User viewer sees local time, same as Admin.</summary>
    [Fact]
    public void SeesLocalTime_User_True()
    {
        var viewer = new AccountResponse { Nickname = "Alice", Role = Role.User };
        Assert.True(_sut.SeesLocalTime(viewer));
    }

    /// <summary>An Anonymous viewer does not see local time (raw UTC instead).</summary>
    [Fact]
    public void SeesLocalTime_Anonymous_False()
    {
        var anonymous = new AccountResponse { Nickname = null, Role = Role.Anonymous };
        Assert.False(_sut.SeesLocalTime(anonymous));
    }

    /// <summary>A null role (unauthenticated default) does not see local time.</summary>
    [Fact]
    public void SeesLocalTime_NullRole_False()
    {
        var viewer = new AccountResponse { Nickname = null, Role = null };
        Assert.False(_sut.SeesLocalTime(viewer));
    }

    // -- GetAdminNicknames -------------------------------------------------------

    /// <summary>To admin nickname set returns only admin nicknames.</summary>
    [Fact]
    public void GetAdminNicknames_ReturnsOnlyAdminNicknames()
    {
        var accounts = new[]
        {
            new AccountResponse { Nickname = "Alice", Role = Role.Admin },
            new AccountResponse { Nickname = "Bob", Role = Role.User },
            new AccountResponse { Nickname = "Carol", Role = Role.Admin }
        };

        var result = _sut.GetAdminNicknames(accounts);

        Assert.Equal([
            "Alice", "Carol"
        ], result);
    }

    /// <summary>To admin nickname set excludes admins with null or empty nickname.</summary>
    [Fact]
    public void GetAdminNicknames_ExcludesAdminsWithNullOrEmptyNickname()
    {
        var accounts = new[]
        {
            new AccountResponse { Nickname = null, Role = Role.Admin },
            new AccountResponse { Nickname = "", Role = Role.Admin },
            new AccountResponse { Nickname = "Dave", Role = Role.Admin }
        };

        var result = _sut.GetAdminNicknames(accounts);

        Assert.Equal([
            "Dave"
        ], result);
    }

    /// <summary>To admin nickname set empty collection returns empty set.</summary>
    [Fact]
    public void GetAdminNicknames_EmptyCollection_ReturnsEmptySet()
    {
        var result = _sut.GetAdminNicknames(Array.Empty<AccountResponse>());

        Assert.Empty(result);
    }

    /// <summary>The byte limit counts bytes, not characters.</summary>
    [Fact]
    public void IsPasswordWithinMaxByteLength_CountsBytes()
    {
        Assert.True(_sut.IsPasswordWithinMaxByteLength(new string('a', 72)));
        Assert.False(_sut.IsPasswordWithinMaxByteLength(new string('가', 25)));
    }

    /// <summary>A session holds while the account exists, is not deleted and keeps its stamp.</summary>
    [Fact]
    public void GetSessionInvalidationReason_NamesWhyASessionEnded()
    {
        AccountResponse session = new() { SecurityStamp = "a" };

        Assert.Null(_sut.GetSessionInvalidationReason(new AccountResponse { SecurityStamp = "a" }, session));
        Assert.Equal("AccountMissing", _sut.GetSessionInvalidationReason(null, session));
        Assert.Equal("AccountDeleted", _sut.GetSessionInvalidationReason(new AccountResponse { SecurityStamp = "a", Deleted = true }, session));
        Assert.Equal("SecurityStampChanged", _sut.GetSessionInvalidationReason(new AccountResponse { SecurityStamp = "b" }, session));
    }
}
