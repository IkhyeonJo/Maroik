using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Dispatches domain events collected by an aggregate root to all registered handlers.
/// Call after the aggregate has been persisted (outside any UoW transaction scope) so that
/// handlers can safely perform their own reads/writes.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
