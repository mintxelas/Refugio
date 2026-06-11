using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Common;
using Refugio.Domain.Repositories;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure.Repositories;

/// <summary>
/// EF base implementation of <see cref="IRepository{T}"/>. Soft delete is the default:
/// Remove() is converted by the SoftDeleteInterceptor; RemovePermanently() arms the
/// skip flag so the next save hard-deletes.
/// </summary>
public abstract class EfRepository<T>(ShelterDbContext db) : IRepository<T> where T : Entity, IAggregateRoot
{
    protected ShelterDbContext Db => db;
    protected DbSet<T> Set => db.Set<T>();

    public virtual async Task<T?> GetAsync(int id) => await Set.FindAsync(id);

    public void Add(T entity) => Set.Add(entity);

    public void Remove(T entity) => Set.Remove(entity);

    public void RemovePermanently(T entity)
    {
        db.SkipSoftDeleteInterceptor = true;
        Set.Remove(entity);
    }

    public virtual Task<List<T>> GetDeletedAsync()
        => DeletedQuery().OrderByDescending(e => e.DeletedAt).ToListAsync();

    public virtual Task<T?> GetDeletedByIdAsync(int id)
        => DeletedQuery().FirstOrDefaultAsync(e => e.Id == id);

    protected IQueryable<T> DeletedQuery()
        => Set.IgnoreQueryFilters().Where(e => e.DeletedAt != null);
}

public static class QueryableExtensions
{
    /// <summary>Runs the count + skip/take needed for one page of <paramref name="query"/>.</summary>
    public static async Task<Page<T>> ToPageAsync<T>(this IQueryable<T> query, int page, int pageSize)
    {
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new Page<T>(items, total, page, pageSize);
    }
}
