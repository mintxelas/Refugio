using Microsoft.EntityFrameworkCore;
using Refugio.Application.Messages;

namespace Refugio.Application;

public static class QueryableExtensions
{
    /// <summary>
    /// Runs the count + skip/take needed for one page of <paramref name="query"/>.
    /// Centralizes the pagination arithmetic that every paged actor handler repeated.
    /// </summary>
    public static async Task<Page<T>> ToPageAsync<T>(this IQueryable<T> query, int page, int pageSize)
    {
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new Page<T>(items, total, page, pageSize);
    }
}
