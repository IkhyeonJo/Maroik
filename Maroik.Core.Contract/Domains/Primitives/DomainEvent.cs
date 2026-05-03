namespace Maroik.Core.Contract.Domains.Primitives;

/// <summary>
/// Base record for all domain events.
/// Using <c>record</c> guarantees structural immutability and value equality for event types.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
