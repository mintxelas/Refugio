using Refugio.Domain.Common;
using Refugio.Domain.Entities;

namespace Refugio.Domain.Repositories;

public interface IAdoptionRepository : IRepository<Adoption>
{
    /// <summary>All adoptions (optionally by status), newest first, dog included.</summary>
    Task<List<Adoption>> GetAllAsync(AdoptionStatus? status = null);
    Task<Page<Adoption>> GetPagedAsync(AdoptionStatus? status, int page, int pageSize);
    /// <summary>Single adoption with its dog and photos, untracked (read view).</summary>
    Task<Adoption?> GetWithDogAsync(int id);

    // Photos
    Task<List<AdoptionPhoto>> GetPhotosAsync(int adoptionId);
    Task<AdoptionPhoto?> GetPhotoAsync(int photoId);
    void AddPhoto(AdoptionPhoto photo);
    void RemovePhotoPermanently(AdoptionPhoto photo);
}
