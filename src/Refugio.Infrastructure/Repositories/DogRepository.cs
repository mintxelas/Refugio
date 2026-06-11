using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure.Repositories;

public class DogRepository(ShelterDbContext db) : EfRepository<Dog>(db), IDogRepository
{
    private static IQueryable<Dog> Filter(IQueryable<Dog> query, string? search, DogStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Name.Contains(search) || d.Breed.Contains(search));
        if (status.HasValue)
            query = query.Where(d => d.Status == status);
        return query;
    }

    public Task<List<Dog>> SearchAsync(string? search, DogStatus? status)
        => Filter(Db.Dogs, search, status).OrderBy(d => d.Name).ToListAsync();

    public Task<Page<Dog>> SearchPagedAsync(string? search, DogStatus? status, int page, int pageSize)
        => Filter(Db.Dogs, search, status).OrderBy(d => d.Name).ToPageAsync(page, pageSize);

    public Task<Dog?> GetWithMedicalHistoryAsync(int id)
        => Db.Dogs
            .Include(d => d.MedicalRecords)
            .Include(d => d.Medications)
            .FirstOrDefaultAsync(d => d.Id == id);

    public Task<bool> ExistsAsync(int dogId) => Db.Dogs.AnyAsync(d => d.Id == dogId);

    public override Task<Dog?> GetDeletedByIdAsync(int id)
        => DeletedQuery()
            .Include(d => d.MedicalRecords)
            .Include(d => d.Medications)
            .FirstOrDefaultAsync(d => d.Id == id);

    // --- Photos ---

    public Task<List<DogPhoto>> GetPhotosAsync(int dogId)
        => Db.DogPhotos
            .Where(p => p.DogId == dogId)
            .OrderByDescending(p => p.IsDefault)
            .ThenByDescending(p => p.UploadedAt)
            .ToListAsync();

    public Task<DogPhoto?> GetPhotoAsync(int photoId)
        => Db.DogPhotos.FirstOrDefaultAsync(p => p.Id == photoId);

    public Task<bool> HasPhotosAsync(int dogId)
        => Db.DogPhotos.AnyAsync(p => p.DogId == dogId);

    public Task<DogPhoto?> GetLatestPhotoAsync(int dogId)
        => Db.DogPhotos
            .Where(p => p.DogId == dogId)
            .OrderByDescending(p => p.UploadedAt)
            .FirstOrDefaultAsync();

    public void AddPhoto(DogPhoto photo) => Db.DogPhotos.Add(photo);

    public void RemovePhotoPermanently(DogPhoto photo)
    {
        Db.SkipSoftDeleteInterceptor = true;
        Db.DogPhotos.Remove(photo);
    }

    // --- Medical records ---

    public Task<List<MedicalRecord>> GetMedicalRecordsAsync(int dogId)
        => Db.MedicalRecords
            .Where(r => r.DogId == dogId)
            .OrderByDescending(r => r.VisitDate)
            .ToListAsync();

    public Task<MedicalRecord?> GetMedicalRecordAsync(int id)
        => Db.MedicalRecords.FirstOrDefaultAsync(r => r.Id == id);

    public void AddMedicalRecord(MedicalRecord record) => Db.MedicalRecords.Add(record);

    public void RemoveMedicalRecord(MedicalRecord record) => Db.MedicalRecords.Remove(record);

    public void RemoveMedicalRecordPermanently(MedicalRecord record)
    {
        Db.SkipSoftDeleteInterceptor = true;
        Db.MedicalRecords.Remove(record);
    }

    public Task<List<MedicalRecord>> GetDeletedMedicalRecordsAsync()
        => Db.MedicalRecords.IgnoreQueryFilters()
            .Include(r => r.Dog)
            .Where(r => r.DeletedAt != null)
            .OrderByDescending(r => r.DeletedAt)
            .ToListAsync();

    public Task<MedicalRecord?> GetDeletedMedicalRecordByIdAsync(int id)
        => Db.MedicalRecords.IgnoreQueryFilters()
            .Include(r => r.Dog)
            .Where(r => r.Id == id && r.DeletedAt != null)
            .FirstOrDefaultAsync();

    // --- Medications ---

    public Task<List<Medication>> GetMedicationsAsync(int dogId)
        => Db.Medications
            .Where(m => m.DogId == dogId)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync();

    public Task<Medication?> GetMedicationAsync(int id)
        => Db.Medications.FirstOrDefaultAsync(m => m.Id == id);

    public void AddMedication(Medication medication) => Db.Medications.Add(medication);

    public void RemoveMedication(Medication medication) => Db.Medications.Remove(medication);

    public void RemoveMedicationPermanently(Medication medication)
    {
        Db.SkipSoftDeleteInterceptor = true;
        Db.Medications.Remove(medication);
    }

    public Task<List<Medication>> GetDeletedMedicationsAsync()
        => Db.Medications.IgnoreQueryFilters()
            .Include(m => m.Dog)
            .Where(m => m.DeletedAt != null)
            .OrderByDescending(m => m.DeletedAt)
            .ToListAsync();

    public Task<Medication?> GetDeletedMedicationByIdAsync(int id)
        => Db.Medications.IgnoreQueryFilters()
            .Include(m => m.Dog)
            .Where(m => m.Id == id && m.DeletedAt != null)
            .FirstOrDefaultAsync();
}
