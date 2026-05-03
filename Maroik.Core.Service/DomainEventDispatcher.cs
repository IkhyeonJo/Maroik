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

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            IEnumerable<object?> handlers = _serviceProvider.GetServices(handlerType);
            foreach (object? handler in handlers)
            {
                if (handler == null) continue;
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
