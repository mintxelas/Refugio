using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Domain.Entities;
using Refugio.Infrastructure.Data;

namespace Refugio.Application.Actors;

/// <summary>
/// Shared base for the domain actors. Owns the scope-per-message pattern and the
/// soft-delete CRUD shapes (delete / restore / list-deleted) that every actor
/// repeated. Actors only write the handlers that carry real domain logic.
/// </summary>
public abstract class ShelterActorBase : ReceiveActor
{
    private readonly IServiceScopeFactory _scopeFactory;

    protected ShelterActorBase(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    /// <summary>Runs <paramref name="work"/> with a fresh scoped DbContext.</summary>
    protected async Task WithDb(Func<ShelterDbContext, Task> work)
    {
        using var scope = _scopeFactory.CreateScope();
        await work(scope.ServiceProvider.GetRequiredService<ShelterDbContext>());
    }

    /// <summary>
    /// Like <see cref="WithDb(Func{ShelterDbContext, Task})"/> but also hands the scoped
    /// <see cref="IServiceProvider"/> to <paramref name="work"/>, for handlers that need
    /// other scoped services (e.g. the email sender) alongside the DbContext.
    /// </summary>
    protected async Task WithDb(Func<ShelterDbContext, IServiceProvider, Task> work)
    {
        using var scope = _scopeFactory.CreateScope();
        await work(scope.ServiceProvider.GetRequiredService<ShelterDbContext>(), scope.ServiceProvider);
    }

    /// <summary>Soft-deletes <typeparamref name="T"/> by id (via the SoftDeleteInterceptor). Replies bool.</summary>
    protected Task SoftDelete<T>(int id) where T : class, ISoftDeletable
    {
        var sender = Sender;
        return WithDb(async db =>
        {
            var entity = await db.Set<T>().FindAsync(id);
            if (entity is null) { sender.Tell(false); return; }
            db.Set<T>().Remove(entity);
            await db.SaveChangesAsync();
            sender.Tell(true);
        });
    }

    /// <summary>
    /// Restores a soft-deleted <typeparamref name="T"/> by id. Replies bool.
    /// <paramref name="canRestore"/> guards restore (e.g. a medical record may only be
    /// restored when its parent dog is still alive); return false to refuse.
    /// </summary>
    protected Task Restore<T>(int id, Func<ShelterDbContext, T, Task<bool>>? canRestore = null)
        where T : class, ISoftDeletable
    {
        var sender = Sender;
        return WithDb(async db =>
        {
            var entity = await db.Set<T>().IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
            if (entity is null) { sender.Tell(false); return; }
            if (canRestore is not null && !await canRestore(db, entity)) { sender.Tell(false); return; }
            entity.DeletedAt = null;
            await db.SaveChangesAsync();
            sender.Tell(true);
        });
    }

    /// <summary>
    /// Replies with all soft-deleted <typeparamref name="T"/>, newest first.
    /// <paramref name="include"/> can add eager loads (run on an IgnoreQueryFilters
    /// query, so it also pulls related rows that are themselves soft-deleted).
    /// </summary>
    protected Task GetDeleted<T>(Func<IQueryable<T>, IQueryable<T>>? include = null)
        where T : class, ISoftDeletable
    {
        var sender = Sender;
        return WithDb(async db =>
        {
            IQueryable<T> query = db.Set<T>().IgnoreQueryFilters();
            if (include is not null) query = include(query);
            var items = await query
                .Where(e => e.DeletedAt != null)
                .OrderByDescending(e => e.DeletedAt)
                .ToListAsync();
            sender.Tell(items);
        });
    }
}
