using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.Board.Events;

/// <summary>
/// Raised when a comment is successfully added to a board post.
/// </summary>
public sealed record BoardCommentAddedEvent(
    long BoardId,
    long CommentId,
    string Writer) : DomainEvent;
