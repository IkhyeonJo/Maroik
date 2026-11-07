using Maroik.Core.Contract.Dtos;
using Maroik.Core.Service.Services;
using Maroik.Core.Domain.Accounts;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="PolicyService"/> — the owner / owner-or-admin
/// authorization rules used to gate edit/delete UI affordances for board posts, comments,
/// and private notes.
/// </summary>
public class PolicyServiceAccountRulesTests
{
    /// <summary>The service under test.</summary>
    private readonly PolicyService _sut = new();

    // -- IsOwner ----------------------------------------------------------------

    /// <summary>Is owner same nickname true.</summary>
    [Fact]
    public void IsOwner_SameNickname_True()
    {
        var viewer = new AccountResponse { Nickname = "Alice", Role = Role.User };
        Assert.True(_sut.IsContentOwner(viewer, "Alice"));
    }

    /// <summary>Is owner different nickname false.</summary>
    [Fact]
    public void IsOwner_DifferentNickname_False()
    {
        var viewer = new AccountResponse { Nickname = "Alice", Role = Role.User };
        Assert.False(_sut.IsContentOwner(viewer, "Bob"));
    }

    /// <summary>Is owner admin but not writer false.</summary>
    [Fact]
    public void IsOwner_AdminButNotWriter_False()
    {
        // Unlike IsOwnerOrAdmin, IsOwner never grants access based on role alone.
        var admin = new AccountResponse { Nickname = "AdminUser", Role = Role.Admin };
        Assert.False(_sut.IsContentOwner(admin, "Bob"));
    }

    /// <summary>Is owner null viewer nickname false.</summary>
    [Fact]
    public void IsOwner_NullViewerNickname_False()
    {
        var anonymous = new AccountResponse { Nickname = null, Role = Role.Anonymous };
        Assert.False(_sut.IsContentOwner(anonymous, "Bob"));
    }

    // -- IsOwnerOrAdmin -----------------------------------------------------------

    /// <summary>Is owner or admin same nickname true.</summary>
    [Fact]
    public void IsOwnerOrAdmin_SameNickname_True()
    {
        var viewer = new AccountResponse { Nickname = "Alice", Role = Role.User };
        Assert.True(_sut.IsContentOwnerOrAdmin(viewer, "Alice"));
    }

    /// <summary>Is owner or admin not writer true.</summary>
    [Fact]
    public void IsOwnerOrAdmin_AdminNotWriter_True()
    {
        var admin = new AccountResponse { Nickname = "AdminUser", Role = Role.Admin };
        Assert.True(_sut.IsContentOwnerOrAdmin(admin, "Bob"));
    }

    /// <summary>Is owner or admin regular user not writer false.</summary>
    [Fact]
    public void IsOwnerOrAdmin_RegularUserNotWriter_False()
    {
        var viewer = new AccountResponse { Nickname = "Alice", Role = Role.User };
        Assert.False(_sut.IsContentOwnerOrAdmin(viewer, "Bob"));
    }

    /// <summary>Is owner or admin anonymous not writer false.</summary>
    [Fact]
    public void IsOwnerOrAdmin_AnonymousNotWriter_False()
    {
        var anonymous = new AccountResponse { Nickname = null, Role = Role.Anonymous };
        Assert.False(_sut.IsContentOwnerOrAdmin(anonymous, "Bob"));
    }

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
}
