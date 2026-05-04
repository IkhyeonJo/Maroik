using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Core.Service;

/// <summary>
/// Resolves <see cref="IDomainEventHandler{TEvent}"/> instances from the DI container
/// and invokes them for each pending domain event.
/// If no handler is registered for a given event type the event is silently ignored.
/// </summary>
internal sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Initializes a new instance of <see cref="DomainEventDispatcher"/> with the supplied service provider.</summary>
    /// <param name="serviceProvider">The application DI service provider used to resolve event handlers.</param>
    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Dispatches each domain event in <paramref name="domainEvents"/> to every registered handler.
    /// Handlers are resolved from the DI container via open-generic <see cref="IDomainEventHandler{TEvent}"/>.
    /// The handler's <c>HandleAsync</c> method is invoked via reflection to support closed generic dispatch.
    /// </summary>
    /// <param name="domainEvents">The list of domain events to dispatch.</param>
    /// <param name="cancellationToken">Optional cancellation token propagated to each handler.</param>
    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            // Build the closed generic handler type for this specific event type at runtime.
            Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());

            // Resolve all registered handlers for this event type (may be empty if none registered).
            IEnumerable<object?> handlers = _serviceProvider.GetServices(handlerType);

            foreach (object? handler in handlers)
            {
                if (handler == null) continue;

                // Invoke HandleAsync via reflection because the closed generic type is not known at compile time.
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
