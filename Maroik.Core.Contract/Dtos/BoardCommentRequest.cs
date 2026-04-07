namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Data transfer object used to create or soft-delete a comment on a board post.
/// </summary>
public class BoardCommentRequest
{
    /// <summary>Comment ID (auto-incremented primary key; 0 for new comments).</summary>
    public long Id { get; set; }

    /// <summary>ID of the parent board post this comment belongs to.</summary>
    public long BoardId { get; set; }

    /// <summary>Display order of the comment within the post.</summary>
    public long Order { get; set; }

    /// <summary>Relative path to the commenter's avatar image.</summary>
    public string? AvatarImagePath { get; set; }

    /// <summary>Nickname of the comment author.</summary>
    public string? Writer { get; set; }

    /// <summary>Comment body text.</summary>
    public string? Content { get; set; }

    /// <summary>UTC timestamp when the comment was posted.</summary>
    public DateTime Created { get; set; }

    /// <summary>Soft-delete flag.</summary>
    public bool Deleted { get; set; }
}
