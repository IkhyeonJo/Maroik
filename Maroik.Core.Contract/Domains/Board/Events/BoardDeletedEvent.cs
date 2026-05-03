using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.Board.Events;

/// <summary>
/// Raised when a board post is soft-deleted.
/// </summary>
public sealed record BoardDeletedEvent(
    long BoardId,
    string Writer) : DomainEvent;
