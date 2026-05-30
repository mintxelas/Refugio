using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Refugio.Domain.Entities;

namespace Refugio.Infrastructure.Data;

/// <summary>
/// Turns a hard <c>Remove()</c> of any <see cref="ISoftDeletable"/> entity into a
/// soft delete (sets <see cref="ISoftDeletable.DeletedAt"/>). This makes soft delete
/// the default policy at the persistence layer, so callers express intent with
/// <c>db.X.Remove(e)</c> and never set the timestamp by hand.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Apply(DbContext? context)
    {
        if (context is null) return;
        if (context is ShelterDbContext sc && sc.SkipSoftDeleteInterceptor)
        {
            sc.SkipSoftDeleteInterceptor = false;
            return;
        }
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted) continue;
            entry.State = EntityState.Modified;
            entry.Entity.DeletedAt = DateTime.UtcNow;
        }
    }
}
