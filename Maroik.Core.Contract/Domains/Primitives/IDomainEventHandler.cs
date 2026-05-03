namespace Maroik.Core.Contract.Domains.Primitives;

/// <summary>
/// Marker interface for handlers of a specific domain event type.
/// Implement this interface to subscribe to <typeparamref name="TEvent"/> dispatched by
/// <c>IDomainEventDispatcher</c> after an aggregate is persisted.
/// </summary>
/// <typeparam name="TEvent">The domain event type this handler processes.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
