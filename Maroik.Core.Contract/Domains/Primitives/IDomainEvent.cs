namespace Maroik.Core.Contract.Domains.Primitives;

/// <summary>
/// Marker interface for all domain events raised by aggregate roots.
/// Domain events capture something meaningful that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Unique identifier for this event instance.</summary>
    Guid EventId { get; }

    /// <summary>UTC timestamp of when the event occurred.</summary>
    DateTime OccurredAt { get; }
}
