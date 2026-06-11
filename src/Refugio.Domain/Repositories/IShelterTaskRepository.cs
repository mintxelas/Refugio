using Refugio.Domain.Entities;

namespace Refugio.Domain.Repositories;

public interface IShelterTaskRepository : IRepository<ShelterTask>
{
    /// <summary>Tasks ordered by due date, assigned volunteer included; completed ones only when asked.</summary>
    Task<List<ShelterTask>> GetAllAsync(bool includeCompleted);
}
