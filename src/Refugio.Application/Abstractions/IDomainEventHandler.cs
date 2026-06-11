using Refugio.Domain.Common;

namespace Refugio.Application.Abstractions;

/// <summary>Handles one domain event type. Resolved from DI by the dispatcher after save.</summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>Dispatches the domain events collected by the unit of work after a successful save.</summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default);
}
