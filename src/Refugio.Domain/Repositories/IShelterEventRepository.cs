using Refugio.Domain.Entities;

namespace Refugio.Domain.Repositories;

public interface IShelterEventRepository : IRepository<ShelterEvent>
{
    Task<List<ShelterEvent>> GetAllAsync(DateTime? from = null, DateTime? to = null);
}
