using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.Board;

/// <summary>
/// Aggregate root for a board post (FreeForum or PrivateNote).
/// Owns its comments and attached files as child entities.
/// </summary>
public sealed class Board : AggregateRoot<long>
{
    private readonly List<BoardComment> _comments = [];
    private readonly List<BoardAttachedFile> _attachedFiles = [];

    /// <summary>Board type identifier (e.g. "FreeForum", "PrivateNote").</summary>
    public string Type { get; private set; }

    /// <summary>Post title.</summary>
    public string Title { get; private set; }

    /// <summary>Post body content in Summernote HTML format.</summary>
    public string? Content { get; private set; }

    /// <summary>Nickname of the author.</summary>
    public string Writer { get; private set; }

    /// <summary>UTC timestamp when the post was created.</summary>
    public DateTime Created { get; private set; }

    /// <summary>UTC timestamp of the most recent edit.</summary>
    public DateTime Updated { get; private set; }

    /// <summary>View count (incremented each time the post is opened).</summary>
    public long View { get; private set; }

    /// <summary>Soft-delete flag.</summary>
    public bool Deleted { get; private set; }

    /// <summary>When true, no new comments can be added.</summary>
    public bool Locked { get; private set; }

    /// <summary>When true, the post is pinned at the top of the list as a notice.</summary>
    public bool Noticed { get; private set; }

    /// <summary>Read-only view of comments on this post.</summary>
    public IReadOnlyList<BoardComment> Comments => _comments.AsReadOnly();

    /// <summary>Read-only view of files attached to this post.</summary>
    public IReadOnlyList<BoardAttachedFile> AttachedFiles => _attachedFiles.AsReadOnly();

    private Board(
        long id,
        string type,
        string title,
        string? content,
        string writer) : base(id)
    {
        Type = type;
        Title = title;
        Content = content;
        Writer = writer;
        Created = DateTime.UtcNow;
        Updated = DateTime.UtcNow;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Factory / Reconstitution
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds a <see cref="Board"/> from trusted raw values from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// Does NOT raise domain events. Child comments and attached files must be loaded separately.
    /// </summary>
    public static Board Reconstitute(
        long id,
        string type,
        string title,
        string? content,
        string writer,
        DateTime created,
        DateTime updated,
        long view,
        bool deleted,
        bool locked,
        bool noticed)
    {
        var board = new Board(id, type, title, content, writer);

        board.Created = created;
        board.Updated = updated;
        board.View = view;
        board.Deleted = deleted;
        board.Locked = locked;
        board.Noticed = noticed;
        return board;
    }

    /// <summary>
    /// Creates a new board post and raises <see cref="BoardCreatedEvent"/>.
    /// </summary>
    public static ErrorOr<Board> Create(
        string? type,
        string? title,
        string? content,
        string? writer)
    {
        if (string.IsNullOrWhiteSpace(type))
            return Error.Validation("Board.TypeEmpty", "Board type cannot be empty.");

        if (string.IsNullOrWhiteSpace(title))
            return Error.Validation("Board.TitleEmpty", "Post title cannot be empty.");

        if (title.Length > 100)
            return Error.Validation("Board.TitleTooLong", "Post title must be 100 characters or fewer.");

        if (content?.Length > 16384)
            return Error.Validation("Board.ContentTooLong", "Post content must be 16384 characters or fewer.");

        if (string.IsNullOrWhiteSpace(writer))
            return Error.Validation("Board.WriterEmpty", "Post author (writer) cannot be empty.");

        var board = new Board(0, type, title, content, writer);
        return board;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Domain behaviours
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>Updates the post title and content.</summary>
    public ErrorOr<Success> Update(string? title, string? content)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Error.Validation("Board.TitleEmpty", "Post title cannot be empty.");

        if (title.Length > 100)
            return Error.Validation("Board.TitleTooLong", "Post title must be 100 characters or fewer.");

        if (content?.Length > 16384)
            return Error.Validation("Board.ContentTooLong", "Post content must be 16384 characters or fewer.");

        if (Deleted)
            return Error.Conflict("Board.Deleted", "Cannot update a deleted post.");

        Title = title;
        Content = content;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Increments the view counter by one.</summary>
    public void IncrementView()
    {
        View++;
    }

    /// <summary>
    /// Adds a comment to this post.
    /// Raises <see cref="BoardCommentAddedEvent"/>.
    /// </summary>
    public ErrorOr<Success> AddComment(BoardComment comment)
    {
        if (Locked)
            return Error.Conflict("Board.Locked", "Cannot add a comment to a locked post.");

        if (Deleted)
            return Error.Conflict("Board.Deleted", "Cannot add a comment to a deleted post.");

        if (comment.BoardId != Id)
            return Error.Validation("Board.CommentMismatch", "Comment does not belong to this post.");

        _comments.Add(comment);
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Soft-deletes the comment with the given ID.</summary>
    public ErrorOr<Success> DeleteComment(long commentId)
    {
        var comment = _comments.Find(c => c.Id == commentId);
        if (comment is null)
            return Error.NotFound("Board.CommentNotFound", $"Comment {commentId} not found on this post.");

        comment.SoftDelete();
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Attaches a file to this post.</summary>
    public ErrorOr<Success> AttachFile(BoardAttachedFile file)
    {
        if (file.BoardId != Id)
            return Error.Validation("Board.FileMismatch", "Attached file does not belong to this post.");

        _attachedFiles.Add(file);
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Locks the post, preventing new comments.</summary>
    public void Lock()
    {
        Locked = true;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Unlocks the post, re-enabling comments.</summary>
    public void Unlock()
    {
        Locked = false;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Pins the post at the top of the list as a notice.</summary>
    public void Pin()
    {
        Noticed = true;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Unpins the post from the notice position.</summary>
    public void Unpin()
    {
        Noticed = false;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Marks the post as soft-deleted and raises <see cref="BoardDeletedEvent"/>.</summary>
    public ErrorOr<Success> SoftDelete()
    {
        if (Deleted)
            return Error.Conflict("Board.AlreadyDeleted", "Post is already deleted.");

        Deleted = true;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }
}
