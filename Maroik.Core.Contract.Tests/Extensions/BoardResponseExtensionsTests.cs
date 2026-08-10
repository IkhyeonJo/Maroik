using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Extensions;

namespace Maroik.Core.Contract.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="BoardResponseExtensions"/> — the "can this viewer see a
/// locked post's title" rule used by the FreeForum and PrivateNote board list views.
/// </summary>
public class BoardResponseExtensionsTests
{
    // -- IsTitleVisibleToOwnerOrAdmin ---------------------------------------------

    /// <summary>Is title visible to owner or admin not locked any viewer true.</summary>
    [Fact]
    public void IsTitleVisibleToOwnerOrAdmin_NotLocked_AnyViewer_True()
    {
        var board = new BoardResponse { Locked = false, Writer = "Alice" };
        var viewer = new AccountResponse { Nickname = "Bob", Role = Role.User };

        Assert.True(board.IsTitleVisibleToOwnerOrAdmin(viewer));
    }

    /// <summary>Is title visible to owner or admin locked and writer true.</summary>
    [Fact]
    public void IsTitleVisibleToOwnerOrAdmin_LockedAndWriter_True()
    {
        var board = new BoardResponse { Locked = true, Writer = "Alice" };
        var viewer = new AccountResponse { Nickname = "Alice", Role = Role.User };

        Assert.True(board.IsTitleVisibleToOwnerOrAdmin(viewer));
    }

    /// <summary>Is title visible to owner or admin locked and admin not writer true.</summary>
    [Fact]
    public void IsTitleVisibleToOwnerOrAdmin_LockedAndAdminNotWriter_True()
    {
        var board = new BoardResponse { Locked = true, Writer = "Alice" };
        var admin = new AccountResponse { Nickname = "AdminUser", Role = Role.Admin };

        Assert.True(board.IsTitleVisibleToOwnerOrAdmin(admin));
    }

    /// <summary>Is title visible to owner or admin locked and regular user not writer false.</summary>
    [Fact]
    public void IsTitleVisibleToOwnerOrAdmin_LockedAndRegularUserNotWriter_False()
    {
        var board = new BoardResponse { Locked = true, Writer = "Alice" };
        var viewer = new AccountResponse { Nickname = "Bob", Role = Role.User };

        Assert.False(board.IsTitleVisibleToOwnerOrAdmin(viewer));
    }

    /// <summary>Is title visible to owner or admin locked and anonymous false.</summary>
    [Fact]
    public void IsTitleVisibleToOwnerOrAdmin_LockedAndAnonymous_False()
    {
        var board = new BoardResponse { Locked = true, Writer = "Alice" };
        var anonymous = new AccountResponse { Nickname = null, Role = Role.Anonymous };

        Assert.False(board.IsTitleVisibleToOwnerOrAdmin(anonymous));
    }

    // -- IsTitleVisibleToOwner ------------------------------------------------------

    /// <summary>Is title visible to owner not locked any viewer true.</summary>
    [Fact]
    public void IsTitleVisibleToOwner_NotLocked_AnyViewer_True()
    {
        var board = new BoardResponse { Locked = false, Writer = "Alice" };
        var viewer = new AccountResponse { Nickname = "Bob", Role = Role.User };

        Assert.True(board.IsTitleVisibleToOwner(viewer));
    }

    /// <summary>Is title visible to owner locked and writer true.</summary>
    [Fact]
    public void IsTitleVisibleToOwner_LockedAndWriter_True()
    {
        var board = new BoardResponse { Locked = true, Writer = "Alice" };
        var viewer = new AccountResponse { Nickname = "Alice", Role = Role.User };

        Assert.True(board.IsTitleVisibleToOwner(viewer));
    }

    /// <summary>Is title visible to owner locked and admin not writer false.</summary>
    [Fact]
    public void IsTitleVisibleToOwner_LockedAndAdminNotWriter_False()
    {
        // Unlike IsTitleVisibleToOwnerOrAdmin, admin status alone does not grant access here.
        var board = new BoardResponse { Locked = true, Writer = "Alice" };
        var admin = new AccountResponse { Nickname = "AdminUser", Role = Role.Admin };

        Assert.False(board.IsTitleVisibleToOwner(admin));
    }

    /// <summary>Is title visible to owner locked and regular user not writer false.</summary>
    [Fact]
    public void IsTitleVisibleToOwner_LockedAndRegularUserNotWriter_False()
    {
        var board = new BoardResponse { Locked = true, Writer = "Alice" };
        var viewer = new AccountResponse { Nickname = "Bob", Role = Role.User };

        Assert.False(board.IsTitleVisibleToOwner(viewer));
    }

    /// <summary>Is title visible to owner locked and anonymous false.</summary>
    [Fact]
    public void IsTitleVisibleToOwner_LockedAndAnonymous_False()
    {
        var board = new BoardResponse { Locked = true, Writer = "Alice" };
        var anonymous = new AccountResponse { Nickname = null, Role = Role.Anonymous };

        Assert.False(board.IsTitleVisibleToOwner(anonymous));
    }
}
