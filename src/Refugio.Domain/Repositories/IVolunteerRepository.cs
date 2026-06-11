using Refugio.Domain.Common;
using Refugio.Domain.Entities;

namespace Refugio.Domain.Repositories;

public interface IVolunteerRepository : IRepository<Volunteer>
{
    Task<List<Volunteer>> GetAllAsync(VolunteerStatus? status = null);
    Task<Page<Volunteer>> GetPagedAsync(VolunteerStatus? status, int page, int pageSize);
    /// <summary>The volunteer matching the email who is allowed to log in, or null.</summary>
    Task<Volunteer?> FindLoginCandidateAsync(string email);
}
