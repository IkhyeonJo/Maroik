using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Dispatches domain events collected by an aggregate root to all registered handlers.
/// Call after the aggregate has been persisted (outside any UoW transaction scope) so that
/// handlers can safely perform their own reads/writes.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches each event in <paramref name="domainEvents"/> to all registered handlers.
    /// </summary>
    /// <param name="domainEvents">The ordered list of domain events to dispatch.</param>
    /// <param name="cancellationToken">Optional cancellation token propagated to each handler.</param>
    Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
