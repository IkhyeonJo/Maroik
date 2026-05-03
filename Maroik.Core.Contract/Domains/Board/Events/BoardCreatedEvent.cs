using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.Board.Events;

/// <summary>
/// Raised when a new board post is published.
/// </summary>
public sealed record BoardCreatedEvent(
    long BoardId,
    string BoardType,
    string Title,
    string Writer) : DomainEvent;
