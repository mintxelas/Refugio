using Refugio.Application.Abstractions;
using Refugio.Domain.Common;

namespace Refugio.Infrastructure.Data;

/// <summary>
/// Saves all tracked changes, then dispatches the domain events the saved aggregates
/// raised. Dispatch happens after the save so handlers only ever see committed state.
/// </summary>
public class UnitOfWork(ShelterDbContext db, IDomainEventDispatcher dispatcher) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await db.SaveChangesAsync(cancellationToken);

        var events = db.ChangeTracker.Entries<Entity>()
            .SelectMany(entry => entry.Entity.DequeueDomainEvents())
            .ToList();
        if (events.Count > 0)
            await dispatcher.DispatchAsync(events, cancellationToken);

        return result;
    }
}
