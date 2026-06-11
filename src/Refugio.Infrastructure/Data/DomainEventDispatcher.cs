using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Abstractions;
using Refugio.Domain.Common;

namespace Refugio.Infrastructure.Data;

/// <summary>Resolves every registered IDomainEventHandler&lt;TEvent&gt; from DI and invokes it.</summary>
public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
            foreach (var handler in serviceProvider.GetServices(handlerType))
            {
                if (handler is null) continue;
                await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
