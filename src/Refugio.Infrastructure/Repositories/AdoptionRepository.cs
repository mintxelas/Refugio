using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure.Repositories;

public class AdoptionRepository(ShelterDbContext db) : EfRepository<Adoption>(db), IAdoptionRepository
{
    private IQueryable<Adoption> ReadQuery(AdoptionStatus? status)
    {
        var query = Db.Adoptions.AsNoTracking().Include(a => a.Dog).AsQueryable();
        if (status.HasValue) query = query.Where(a => a.Status == status);
        return query.OrderByDescending(a => a.CreatedAt);
    }

    public Task<List<Adoption>> GetAllAsync(AdoptionStatus? status = null)
        => ReadQuery(status).ToListAsync();

    public Task<Page<Adoption>> GetPagedAsync(AdoptionStatus? status, int page, int pageSize)
        => ReadQuery(status).ToPageAsync(page, pageSize);

    public Task<Adoption?> GetWithDogAsync(int id)
        => Db.Adoptions.AsNoTracking()
            .Include(a => a.Dog)
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id);

    public override Task<List<Adoption>> GetDeletedAsync()
        => DeletedQuery()
            .Include(a => a.Dog)
            .OrderByDescending(a => a.DeletedAt)
            .ToListAsync();

    public override Task<Adoption?> GetDeletedByIdAsync(int id)
        => DeletedQuery()
            .Include(a => a.Dog)
            .FirstOrDefaultAsync(a => a.Id == id);

    public Task<List<AdoptionPhoto>> GetPhotosAsync(int adoptionId)
        => Db.AdoptionPhotos
            .Where(p => p.AdoptionId == adoptionId)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

    public Task<AdoptionPhoto?> GetPhotoAsync(int photoId)
        => Db.AdoptionPhotos.FirstOrDefaultAsync(p => p.Id == photoId);

    public void AddPhoto(AdoptionPhoto photo) => Db.AdoptionPhotos.Add(photo);

    public void RemovePhotoPermanently(AdoptionPhoto photo)
    {
        Db.SkipSoftDeleteInterceptor = true;
        Db.AdoptionPhotos.Remove(photo);
    }
}
