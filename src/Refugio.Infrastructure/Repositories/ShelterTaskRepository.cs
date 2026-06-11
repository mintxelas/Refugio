using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure.Repositories;

public class ShelterTaskRepository(ShelterDbContext db) : EfRepository<ShelterTask>(db), IShelterTaskRepository
{
    public Task<List<ShelterTask>> GetAllAsync(bool includeCompleted)
    {
        var query = Db.Tasks.Include(t => t.AssignedVolunteer).AsQueryable();
        if (!includeCompleted) query = query.Where(t => !t.IsCompleted);
        return query.OrderBy(t => t.DueDateTime).ToListAsync();
    }
}
