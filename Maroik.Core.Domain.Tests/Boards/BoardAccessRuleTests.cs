using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.Boards;

namespace Maroik.Core.Domain.Tests.Boards;

/// <summary>Who may comment on a post and delete a comment: the rules the post answers itself.</summary>
public class BoardAccessRuleTests
{
    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime _now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A post of <paramref name="type"/> by Alice, with id 1.</summary>
    private static Board PostOf(string type) =>
        Board.Reconstitute(1, type, "Title", "Body", "Alice", _now, _now, 0, false, false, false);

    /// <summary>A comment on post 1 by <paramref name="writer"/>.</summary>
    private static BoardComment CommentBy(string writer) => BoardComment.Reconstitute(1, 1, 1, null, writer, "Hi", _now, false);

    /// <summary>Only its author can comment on a private note; an administrator gets no bypass.</summary>
    [Theory]
    [InlineData("Bob", false)]
    [InlineData("Bob", true)]
    [InlineData(null, true)]
    public void AddComment_RefusesAnyoneButTheAuthor_OnAPrivateNote(string? commenter, bool isAdmin)
    {
        var result = PostOf(BoardTypes.PrivateNote).AddComment(CommentBy("Bob"), _now, commenter, isAdmin);

        Assert.True(result.IsError);
        Assert.Equal("Board.PrivateNoteComment", result.FirstError.Code);
    }

    /// <summary>The author comments on their own note, and anyone on an unlocked forum post.</summary>
    [Fact]
    public void AddComment_AcceptsTheAuthor_OnAPrivateNote_AndAnyone_OnAForumPost()
    {
        Assert.False(PostOf(BoardTypes.PrivateNote).AddComment(CommentBy("Alice"), _now, "Alice").IsError);
        Assert.False(PostOf(BoardTypes.FreeForum).AddComment(CommentBy("Bob"), _now, "Bob").IsError);
    }

    /// <summary>An administrator deletes a comment on a forum post but not on a private note; its writer deletes it on either.</summary>
    [Theory]
    [InlineData(BoardTypes.FreeForum, "Admin", true, true)]
    [InlineData(BoardTypes.PrivateNote, "Admin", true, false)]
    [InlineData(BoardTypes.PrivateNote, "Bob", false, true)]
    [InlineData(BoardTypes.FreeForum, "Carol", false, false)]
    public void CanCommentBeDeletedBy_HasNoAdminBypass_OnAPrivateNote(string type, string requester, bool isAdmin, bool expected)
    {
        Assert.Equal(expected, PostOf(type).CanCommentBeDeletedBy(CommentBy("Bob"), requester, isAdmin));
    }

    /// <summary>The writer or any admin qualifies; a blank writer belongs to nobody, and a non-admin stranger is refused.</summary>
    [Theory]
    [InlineData("Alice", "Alice", false, true)]
    [InlineData("Bob", "Alice", true, true)]
    [InlineData("Bob", "Alice", false, false)]
    [InlineData("", "", false, false)]
    public void IsOwnedByOrAdmin_AcceptsTheWriterOrAnAdmin(string writer, string viewer, bool isAdmin, bool expected)
    {
        Assert.Equal(expected, Board.IsOwnedByOrAdmin(writer, viewer, isAdmin));
    }

    /// <summary>Anyone signed in may comment; a visitor may not.</summary>
    [Theory]
    [InlineData(Role.Admin, true)]
    [InlineData(Role.User, true)]
    [InlineData(Role.Anonymous, false)]
    [InlineData(null, false)]
    public void AcceptsCommentsFrom_IsAnyoneSignedIn(string? role, bool expected)
    {
        Assert.Equal(expected, Board.AcceptsCommentsFrom(role));
    }

    /// <summary>Only an administrator may pin.</summary>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void CanBePinnedBy_IsTheAdministrator(bool isAdmin, bool expected)
    {
        Assert.Equal(expected, Board.CanBePinnedBy(isAdmin));
    }
}
