using Maroik.Website.Constants;

namespace Maroik.Website.Tests;

/// <summary>Unit tests for <see cref="BoardListPaging"/>: how the board lists are paged.</summary>
public class BoardListPagingTests
{
    /// <summary>There is no page before the first; any other page stands.</summary>
    [Theory]
    [InlineData(-3, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    public void PageOf_HasNoPageBeforeTheFirst(int requested, int expected)
    {
        Assert.Equal(expected, BoardListPaging.PageOf(requested));
    }
}
