using Maroik.Core.Domain.Boards;
using Maroik.Core.Domain.Primitives;
using Maroik.Core.Domain.Tests.Fixtures;

namespace Maroik.Core.Domain.Tests.Boards;

/// <summary>
/// Exact limits and ownership branches of the board rules: a title, content or attachment AT its limit is accepted and
/// one more is refused, and who may delete a private note or a forum post.
/// </summary>
public class BoardBoundaryTests
{
    /// <summary>The fixed "current time" every domain call in this class receives.</summary>
    private static readonly DateTime _now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A string of exactly <paramref name="length"/> characters.</summary>
    private static string Text(int length) => new('x', length);

    /// <summary>Writer nickname used for boards and comments.</summary>
    private const string Writer = "Alice";

    /// <summary>Verifies that a forum post may be deleted by its writer or an admin, but a private note only by its writer.</summary>
    [Fact]
    public void Board_CanBeDeletedBy_FollowsThePrivateNoteAndForumRules()
    {
        Board forum = Board.Reconstitute(1, BoardTypes.FreeForum, "t", "c", "Alice", DateTime.UtcNow, DateTime.UtcNow, 0L, false, false, false);
        Board note = Board.Reconstitute(2, BoardTypes.PrivateNote, "t", "c", "Alice", DateTime.UtcNow, DateTime.UtcNow, 0L, false, false, false);

        Assert.True(forum.CanBeDeletedBy("Alice", isAdmin: false));
        Assert.True(forum.CanBeDeletedBy("Admin", isAdmin: true));
        Assert.False(forum.CanBeDeletedBy("Bob", isAdmin: false));

        Assert.True(note.CanBeDeletedBy("Alice", isAdmin: false));
        Assert.False(note.CanBeDeletedBy("Admin", isAdmin: true)); // a private note has no admin bypass
        Assert.False(note.CanBeDeletedBy("Bob", isAdmin: false));
    }

    /// <summary>Verifies that <c>Board.Create</c> accepts a title and content at the maximum length and refuses one character more.</summary>
    [Fact]
    public void Board_Create_AcceptsATitleAndContentAtTheLimit_AndRefusesOneMore()
    {
        Assert.False(Board.Create(BoardTypes.FreeForum, Text(TitledContentPolicy.MaxTitleLength), Text(TitledContentPolicy.MaxBodyLength), Writer, _now).IsError);

        ErrorAssert.Validation(Board.Create(BoardTypes.FreeForum, Text(TitledContentPolicy.MaxTitleLength + 1), "c", Writer, _now),
            "Board.TitleTooLong", "Post title must be {0} characters or fewer.", TitledContentPolicy.MaxTitleLength);
        ErrorAssert.Validation(Board.Create(BoardTypes.FreeForum, "t", Text(TitledContentPolicy.MaxBodyLength + 1), Writer, _now),
            "Board.ContentTooLong", "Post content must be {0} characters or fewer.", TitledContentPolicy.MaxBodyLength);
    }

    /// <summary>
    /// The raw (not yet sanitized) body is bounded at <see cref="TitledContentPolicy.MaxRawBodyLength"/> before the sanitizer sees it:
    /// sanitizing deeply nested HTML takes time that grows faster than its length (1.1 MB took 68 s). A raw body over that bound is
    /// refused with the content's own error; one at it passes, and the sanitized content is still held to MaxBodyLength.
    /// </summary>
    [Fact]
    public void TheRawBody_IsBoundedBeforeSanitizing()
    {
        Assert.Equal(4 * TitledContentPolicy.MaxBodyLength, TitledContentPolicy.MaxRawBodyLength);

        Assert.False(Board.ValidateRawContent(Text(TitledContentPolicy.MaxRawBodyLength)).IsError);
        Assert.False(Board.ValidateRawContent(null).IsError);
        ErrorAssert.Validation(Board.ValidateRawContent(Text(TitledContentPolicy.MaxRawBodyLength + 1)),
            "Board.ContentTooLong", "Post content must be {0} characters or fewer.", TitledContentPolicy.MaxBodyLength);

        Assert.False(BoardComment.ValidateRawContent(Text(TitledContentPolicy.MaxRawBodyLength)).IsError);
        ErrorAssert.Validation(BoardComment.ValidateRawContent(Text(TitledContentPolicy.MaxRawBodyLength + 1)),
            "BoardComment.ContentTooLong", "Comment must be {0} characters or fewer.", TitledContentPolicy.MaxBodyLength);
    }

    /// <summary>Verifies that <c>Board.Update</c> accepts a title and content at the maximum length and refuses one character more.</summary>
    [Fact]
    public void Board_Update_AcceptsATitleAndContentAtTheLimit_AndRefusesOneMore()
    {
        Board board = Board.Create(BoardTypes.FreeForum, "t", "c", Writer, _now).Value;

        Assert.False(board.Update(Text(TitledContentPolicy.MaxTitleLength), Text(TitledContentPolicy.MaxBodyLength), _now).IsError);

        ErrorAssert.Validation(board.Update(Text(TitledContentPolicy.MaxTitleLength + 1), "c", _now),
            "Board.TitleTooLong", "Post title must be {0} characters or fewer.", TitledContentPolicy.MaxTitleLength);
        ErrorAssert.Validation(board.Update("t", Text(TitledContentPolicy.MaxBodyLength + 1), _now),
            "Board.ContentTooLong", "Post content must be {0} characters or fewer.", TitledContentPolicy.MaxBodyLength);
    }

    /// <summary>Verifies the empty/unknown board type, empty title and empty writer errors.</summary>
    [Fact]
    public void Board_ValidationMessages_ForEmptyAndUnknownInput()
    {
        ErrorAssert.Validation(Board.Create("", "t", "c", Writer, _now), "Board.TypeEmpty", "Board type cannot be empty.");
        ErrorAssert.Validation(Board.Create("Nonsense", "t", "c", Writer, _now), "Board.TypeInvalid", "Board type is not a recognized value.");
        ErrorAssert.Validation(Board.Create(BoardTypes.FreeForum, " ", "c", Writer, _now), "Board.TitleEmpty", "Post title cannot be empty.");
        ErrorAssert.Validation(Board.Create(BoardTypes.FreeForum, "t", "c", " ", _now), "Board.WriterEmpty", "Post author (writer) cannot be empty.");
        ErrorAssert.Validation(Board.Create(BoardTypes.FreeForum, "t", "c", Writer, _now).Value.Update(" ", "c", _now),
            "Board.TitleEmpty", "Post title cannot be empty.");
    }

    /// <summary>Verifies the size, empty and length limits of a board attachment (a size of 0 is legal).</summary>
    [Fact]
    public void BoardAttachedFile_Create_AcceptsTheLimits_AndRefusesOneMore()
    {
        Assert.False(BoardAttachedFile.Create(1, 0, Text(ShortTextPolicy.MaxLength), Text(ShortTextPolicy.MaxLength), Text(ShortTextPolicy.MaxLength)).IsError); // size 0 is a legal (empty) size

        ErrorAssert.Validation(BoardAttachedFile.Create(1, -1, "n", ".e", "p"), "BoardAttachedFile.InvalidSize", "File size cannot be negative.");
        ErrorAssert.Validation(BoardAttachedFile.Create(1, 1, " ", ".e", "p"), "BoardAttachedFile.NameEmpty", "File name cannot be empty.");
        ErrorAssert.Validation(BoardAttachedFile.Create(1, 1, "n", ".e", " "), "BoardAttachedFile.PathEmpty", "File storage path cannot be empty.");
        ErrorAssert.Validation(BoardAttachedFile.Create(1, 1, Text(ShortTextPolicy.MaxLength + 1), ".e", "p"), "BoardAttachedFile.NameTooLong", "File name must be {0} characters or fewer.", ShortTextPolicy.MaxLength);
        ErrorAssert.Validation(BoardAttachedFile.Create(1, 1, "n", Text(ShortTextPolicy.MaxLength + 1), "p"), "BoardAttachedFile.ExtensionTooLong", "File extension must be {0} characters or fewer.", ShortTextPolicy.MaxLength);
        ErrorAssert.Validation(BoardAttachedFile.Create(1, 1, "n", ".e", Text(ShortTextPolicy.MaxLength + 1)), "BoardAttachedFile.PathTooLong", "File storage path must be {0} characters or fewer.", ShortTextPolicy.MaxLength);
    }
}
