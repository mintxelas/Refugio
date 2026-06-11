using Refugio.Domain.Common;

namespace Refugio.Domain.Repositories;

/// <summary>
/// Base contract for aggregate-root repositories. Repositories never save — call
/// <see cref="IUnitOfWork.SaveChangesAsync"/> to commit (which also dispatches domain events).
/// <see cref="Remove"/> soft-deletes on save (SoftDeleteInterceptor);
/// <see cref="RemovePermanently"/> hard-deletes a soft-deleted row.
/// </summary>
public interface IRepository<T> where T : Entity, IAggregateRoot
{
    Task<T?> GetAsync(int id);
    void Add(T entity);
    void Remove(T entity);
    void RemovePermanently(T entity);
    Task<List<T>> GetDeletedAsync();
    Task<T?> GetDeletedByIdAsync(int id);
}
