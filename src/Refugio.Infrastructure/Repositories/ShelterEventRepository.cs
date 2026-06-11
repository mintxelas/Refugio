using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure.Repositories;

public class ShelterEventRepository(ShelterDbContext db) : EfRepository<ShelterEvent>(db), IShelterEventRepository
{
    public Task<List<ShelterEvent>> GetAllAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = Db.Events.AsQueryable();
        if (from.HasValue) query = query.Where(e => e.StartDateTime >= from);
        if (to.HasValue) query = query.Where(e => e.StartDateTime <= to);
        return query.OrderBy(e => e.StartDateTime).ToListAsync();
    }
}
