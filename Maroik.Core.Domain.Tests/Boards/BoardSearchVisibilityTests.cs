using Maroik.Core.Domain.Boards;
using Maroik.Core.Domain.Primitives;

namespace Maroik.Core.Domain.Tests.Boards;

/// <summary>Which locked posts a board search may return is the domain's decision; the repository only translates it into SQL.</summary>
public class BoardSearchVisibilityTests
{
    /// <summary>What a board search may return depends on who is searching: a visitor, a user, an admin, or an unknown signed-in role.</summary>
    [Theory]
    [InlineData(false, null, BoardSearchVisibility.PublicOnly)]
    [InlineData(false, Role.Admin, BoardSearchVisibility.PublicOnly)] // not signed in: the role is ignored
    [InlineData(true, Role.User, BoardSearchVisibility.PublicAndOwn)]
    [InlineData(true, Role.Admin, BoardSearchVisibility.AllMatches)]
    [InlineData(true, "Ghost", BoardSearchVisibility.Unsearched)]
    [InlineData(true, null, BoardSearchVisibility.Unsearched)]
    public void SearchVisibilityFor_FollowsTheViewer(bool isLoggedIn, string? role, BoardSearchVisibility expected)
    {
        Assert.Equal(expected, Board.SearchVisibilityFor(isLoggedIn, role));
    }
}
