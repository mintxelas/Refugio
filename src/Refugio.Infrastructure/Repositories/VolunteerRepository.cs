using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure.Repositories;

public class VolunteerRepository(ShelterDbContext db) : EfRepository<Volunteer>(db), IVolunteerRepository
{
    private IQueryable<Volunteer> Filter(VolunteerStatus? status)
    {
        var query = Db.Volunteers.AsQueryable();
        if (status.HasValue) query = query.Where(v => v.Status == status);
        return query.OrderBy(v => v.Name);
    }

    public Task<List<Volunteer>> GetAllAsync(VolunteerStatus? status = null)
        => Filter(status).ToListAsync();

    public Task<Page<Volunteer>> GetPagedAsync(VolunteerStatus? status, int page, int pageSize)
        => Filter(status).ToPageAsync(page, pageSize);

    public Task<Volunteer?> FindLoginCandidateAsync(string email)
        => Db.Volunteers.FirstOrDefaultAsync(v => v.Email == email && v.CanLogin);
}
