using Maroik.Core.Contract.Misc.Constants;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Extensions;

namespace Maroik.Core.Contract.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="AccountResponseExtensions"/> — the owner / owner-or-admin
/// authorization rules used to gate edit/delete UI affordances for board posts, comments,
/// and private notes.
/// </summary>
public class AccountResponseExtensionsTests
{
    // -- IsOwner ----------------------------------------------------------------

    [Fact]
    public void IsOwner_SameNickname_True()
    {
        var viewer = new AccountResponse { Nickname = "Alice", Role = Role.User };
        Assert.True(viewer.IsOwner("Alice"));
    }

    [Fact]
    public void IsOwner_DifferentNickname_False()
    {
        var viewer = new AccountResponse { Nickname = "Alice", Role = Role.User };
        Assert.False(viewer.IsOwner("Bob"));
    }

    [Fact]
    public void IsOwner_AdminButNotWriter_False()
    {
        // Unlike IsOwnerOrAdmin, IsOwner never grants access based on role alone.
        var admin = new AccountResponse { Nickname = "AdminUser", Role = Role.Admin };
        Assert.False(admin.IsOwner("Bob"));
    }

    [Fact]
    public void IsOwner_NullViewerNickname_False()
    {
        var anonymous = new AccountResponse { Nickname = null, Role = Role.Anonymous };
        Assert.False(anonymous.IsOwner("Bob"));
    }

    // -- IsOwnerOrAdmin -----------------------------------------------------------

    [Fact]
    public void IsOwnerOrAdmin_SameNickname_True()
    {
        var viewer = new AccountResponse { Nickname = "Alice", Role = Role.User };
        Assert.True(viewer.IsOwnerOrAdmin("Alice"));
    }

    [Fact]
    public void IsOwnerOrAdmin_AdminNotWriter_True()
    {
        var admin = new AccountResponse { Nickname = "AdminUser", Role = Role.Admin };
        Assert.True(admin.IsOwnerOrAdmin("Bob"));
    }

    [Fact]
    public void IsOwnerOrAdmin_RegularUserNotWriter_False()
    {
        var viewer = new AccountResponse { Nickname = "Alice", Role = Role.User };
        Assert.False(viewer.IsOwnerOrAdmin("Bob"));
    }

    [Fact]
    public void IsOwnerOrAdmin_AnonymousNotWriter_False()
    {
        var anonymous = new AccountResponse { Nickname = null, Role = Role.Anonymous };
        Assert.False(anonymous.IsOwnerOrAdmin("Bob"));
    }

    // -- ToAdminNicknameSet -------------------------------------------------------

    [Fact]
    public void ToAdminNicknameSet_ReturnsOnlyAdminNicknames()
    {
        var accounts = new[]
        {
            new AccountResponse { Nickname = "Alice", Role = Role.Admin },
            new AccountResponse { Nickname = "Bob", Role = Role.User },
            new AccountResponse { Nickname = "Carol", Role = Role.Admin }
        };

        var result = accounts.ToAdminNicknameSet();

        Assert.Equal(new HashSet<string> { "Alice", "Carol" }, result);
    }

    [Fact]
    public void ToAdminNicknameSet_ExcludesAdminsWithNullOrEmptyNickname()
    {
        var accounts = new[]
        {
            new AccountResponse { Nickname = null, Role = Role.Admin },
            new AccountResponse { Nickname = "", Role = Role.Admin },
            new AccountResponse { Nickname = "Dave", Role = Role.Admin }
        };

        var result = accounts.ToAdminNicknameSet();

        Assert.Equal(new HashSet<string> { "Dave" }, result);
    }

    [Fact]
    public void ToAdminNicknameSet_EmptyCollection_ReturnsEmptySet()
    {
        var result = Array.Empty<AccountResponse>().ToAdminNicknameSet();

        Assert.Empty(result);
    }
}
