using Refugio.Domain.Common;
using Refugio.Domain.Entities;

namespace Refugio.Domain.Repositories;

/// <summary>
/// Repository for the Dog aggregate. Child entities (medical records, medications,
/// photos) are reached through here — they have no repository of their own.
/// </summary>
public interface IDogRepository : IRepository<Dog>
{
    Task<List<Dog>> SearchAsync(string? search, DogStatus? status);
    Task<Page<Dog>> SearchPagedAsync(string? search, DogStatus? status, int page, int pageSize);
    Task<Dog?> GetWithMedicalHistoryAsync(int id);
    Task<bool> ExistsAsync(int dogId);

    // Photos
    Task<List<DogPhoto>> GetPhotosAsync(int dogId);
    Task<DogPhoto?> GetPhotoAsync(int photoId);
    Task<bool> HasPhotosAsync(int dogId);
    Task<DogPhoto?> GetLatestPhotoAsync(int dogId);
    void AddPhoto(DogPhoto photo);
    void RemovePhotoPermanently(DogPhoto photo);

    // Medical records
    Task<List<MedicalRecord>> GetMedicalRecordsAsync(int dogId);
    Task<MedicalRecord?> GetMedicalRecordAsync(int id);
    void AddMedicalRecord(MedicalRecord record);
    void RemoveMedicalRecord(MedicalRecord record);
    void RemoveMedicalRecordPermanently(MedicalRecord record);
    Task<List<MedicalRecord>> GetDeletedMedicalRecordsAsync();
    Task<MedicalRecord?> GetDeletedMedicalRecordByIdAsync(int id);

    // Medications
    Task<List<Medication>> GetMedicationsAsync(int dogId);
    Task<Medication?> GetMedicationAsync(int id);
    void AddMedication(Medication medication);
    void RemoveMedication(Medication medication);
    void RemoveMedicationPermanently(Medication medication);
    Task<List<Medication>> GetDeletedMedicationsAsync();
    Task<Medication?> GetDeletedMedicationByIdAsync(int id);
}
