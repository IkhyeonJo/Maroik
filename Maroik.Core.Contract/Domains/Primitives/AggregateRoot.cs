namespace Maroik.Core.Contract.Domains.Primitives;

/// <summary>
/// Base class for all aggregate roots.
/// Extends <see cref="Entity{TId}"/> with domain-event publishing.
/// The infrastructure layer dispatches <see cref="DomainEvents"/> after the aggregate is persisted,
/// then calls <see cref="ClearDomainEvents"/> to reset the collection.
/// </summary>
/// <typeparam name="TId">Type of the aggregate's primary key.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Domain events raised during this aggregate's lifetime that have not yet been dispatched.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(TId id) : base(id) { }

    /// <summary>Appends a domain event to the pending event collection.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Atomically returns all pending domain events and clears the collection.
    /// Call this immediately after the aggregate is persisted.
    /// </summary>
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }

    /// <summary>Removes all pending domain events (called by infrastructure after dispatch).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
