using Refugio.Domain.Common;

namespace Refugio.Application.Services;

/// <summary>
/// Shared command shapes for soft delete, restore and purge. Restore and purge only
/// accept entities that are currently soft-deleted (the loader must come from a
/// GetDeleted* repository method).
/// </summary>
public abstract class ShelterServiceBase(IUnitOfWork unitOfWork)
{
    protected IUnitOfWork UnitOfWork => unitOfWork;

    protected async Task<bool> SoftDeleteAsync<T>(Func<Task<T?>> load, Action<T> remove) where T : Entity
    {
        var entity = await load();
        if (entity is null) return false;
        remove(entity);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    protected async Task<bool> RestoreAsync<T>(Func<Task<T?>> loadDeleted) where T : Entity
    {
        var entity = await loadDeleted();
        if (entity is null) return false;
        entity.Restore();
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    protected async Task<bool> PurgeAsync<T>(Func<Task<T?>> loadDeleted, Action<T> removePermanently) where T : Entity
    {
        var entity = await loadDeleted();
        if (entity is null) return false;
        removePermanently(entity);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
