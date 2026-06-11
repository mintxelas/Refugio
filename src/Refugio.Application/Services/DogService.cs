using Refugio.Application.Contracts;
using Refugio.Application.Mapping;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;

namespace Refugio.Application.Services;

public interface IDogService
{
    Task<List<DogDto>> GetDogsAsync(string? search = null, DogStatus? status = null);
    Task<Page<DogDto>> GetDogsPagedAsync(string? search, DogStatus? status, int page, int pageSize = 20);
    Task<DogDto?> GetDogAsync(int id);
    Task<DogDto> CheckInDogAsync(CreateDogRequest request);
    Task<DogDto?> UpdateDogAsync(UpdateDogRequest request);
    Task<bool> DeleteDogAsync(int id);
    Task<bool> RestoreDogAsync(int id);
    Task<bool> PurgeDogAsync(int id);
    Task<List<DogDto>> GetDeletedDogsAsync();
    Task<DogDto?> GetDeletedDogAsync(int id);

    Task<bool> SetDogPhotoAsync(int id, string? photoUrl);
    Task<List<DogPhotoDto>> GetDogPhotosAsync(int dogId);
    Task<DogPhotoDto?> AddDogPhotoAsync(int dogId, string url);
    Task<bool> SetDefaultDogPhotoAsync(int photoId);
    Task<string?> RemoveDogPhotoAsync(int photoId);

    Task<List<MedicalRecordDto>> GetMedicalRecordsAsync(int dogId);
    Task<MedicalRecordDto?> GetMedicalRecordAsync(int id);
    Task<MedicalRecordDto?> AddMedicalRecordAsync(CreateMedicalRecordRequest request);
    Task<MedicalRecordDto?> UpdateMedicalRecordAsync(UpdateMedicalRecordRequest request);
    Task<bool> DeleteMedicalRecordAsync(int id);
    Task<bool> RestoreMedicalRecordAsync(int id);
    Task<bool> PurgeMedicalRecordAsync(int id);
    Task<List<MedicalRecordDto>> GetDeletedMedicalRecordsAsync();
    Task<MedicalRecordDto?> GetDeletedMedicalRecordAsync(int id);

    Task<List<MedicationDto>> GetMedicationsAsync(int dogId);
    Task<MedicationDto?> GetMedicationAsync(int id);
    Task<MedicationDto?> AddMedicationAsync(CreateMedicationRequest request);
    Task<MedicationDto?> UpdateMedicationAsync(UpdateMedicationRequest request);
    Task<bool> DeactivateMedicationAsync(int id);
    Task<bool> DeleteMedicationAsync(int id);
    Task<bool> RestoreMedicationAsync(int id);
    Task<bool> PurgeMedicationAsync(int id);
    Task<List<MedicationDto>> GetDeletedMedicationsAsync();
    Task<MedicationDto?> GetDeletedMedicationAsync(int id);
}

/// <summary>Use cases for the Dog aggregate (dogs, medical records, medications, photo gallery).</summary>
public class DogService(IDogRepository dogs, IUnitOfWork unitOfWork) : ShelterServiceBase(unitOfWork), IDogService
{
    public async Task<List<DogDto>> GetDogsAsync(string? search = null, DogStatus? status = null)
        => (await dogs.SearchAsync(search, status)).Select(d => d.ToDto()).ToList();

    public async Task<Page<DogDto>> GetDogsPagedAsync(string? search, DogStatus? status, int page, int pageSize = 20)
        => (await dogs.SearchPagedAsync(search, status, page, pageSize)).ToDto(d => d.ToDto());

    public async Task<DogDto?> GetDogAsync(int id)
        => (await dogs.GetWithMedicalHistoryAsync(id))?.ToDto();

    public async Task<DogDto> CheckInDogAsync(CreateDogRequest request)
    {
        var dog = Dog.CheckIn(
            request.Name, request.Breed, request.AgeMonths, request.Gender, request.WeightKg,
            request.PhotoUrl, request.Traits, request.Notes);
        dogs.Add(dog);
        await UnitOfWork.SaveChangesAsync();
        return dog.ToDto();
    }

    public async Task<DogDto?> UpdateDogAsync(UpdateDogRequest request)
    {
        var dog = await dogs.GetAsync(request.Id);
        if (dog is null) return null;
        dog.UpdateDetails(
            request.Name, request.Breed, request.AgeMonths, request.Gender, request.Status,
            request.WeightKg, request.PhotoUrl, request.Traits, request.Notes);
        await UnitOfWork.SaveChangesAsync();
        return dog.ToDto();
    }

    public Task<bool> DeleteDogAsync(int id) => SoftDeleteAsync(() => dogs.GetAsync(id), dogs.Remove);

    public Task<bool> RestoreDogAsync(int id) => RestoreAsync(() => dogs.GetDeletedByIdAsync(id));

    public Task<bool> PurgeDogAsync(int id) => PurgeAsync(() => dogs.GetDeletedByIdAsync(id), dogs.RemovePermanently);

    public async Task<List<DogDto>> GetDeletedDogsAsync()
        => (await dogs.GetDeletedAsync()).Select(d => d.ToDto()).ToList();

    public async Task<DogDto?> GetDeletedDogAsync(int id)
        => (await dogs.GetDeletedByIdAsync(id))?.ToDto();

    // --- Photos ---

    public async Task<bool> SetDogPhotoAsync(int id, string? photoUrl)
    {
        var dog = await dogs.GetAsync(id);
        if (dog is null) return false;
        dog.SetPhotoUrl(photoUrl);
        await UnitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<List<DogPhotoDto>> GetDogPhotosAsync(int dogId)
        => (await dogs.GetPhotosAsync(dogId)).Select(p => p.ToDto()).ToList();

    public async Task<DogPhotoDto?> AddDogPhotoAsync(int dogId, string url)
    {
        var dog = await dogs.GetAsync(dogId);
        if (dog is null) return null;
        var isFirst = !await dogs.HasPhotosAsync(dogId);
        var photo = DogPhoto.Create(dogId, url, isDefault: isFirst);
        dogs.AddPhoto(photo);
        if (isFirst) dog.SetPhotoUrl(url);
        await UnitOfWork.SaveChangesAsync();
        return photo.ToDto();
    }

    public async Task<bool> SetDefaultDogPhotoAsync(int photoId)
    {
        var photo = await dogs.GetPhotoAsync(photoId);
        if (photo is null) return false;
        var siblings = await dogs.GetPhotosAsync(photo.DogId);
        foreach (var sibling in siblings) sibling.SetDefault(sibling.Id == photo.Id);
        var dog = await dogs.GetAsync(photo.DogId);
        dog?.SetPhotoUrl(photo.Url);
        await UnitOfWork.SaveChangesAsync();
        return true;
    }

    /// <summary>Hard-deletes the photo row; returns its URL so the caller can remove the file.</summary>
    public async Task<string?> RemoveDogPhotoAsync(int photoId)
    {
        var photo = await dogs.GetPhotoAsync(photoId);
        if (photo is null) return null;
        var (wasDefault, dogId, url) = (photo.IsDefault, photo.DogId, photo.Url);
        dogs.RemovePhotoPermanently(photo);
        await UnitOfWork.SaveChangesAsync();
        if (wasDefault)
        {
            // Promote the next most-recent remaining photo (if any) to default.
            var next = await dogs.GetLatestPhotoAsync(dogId);
            next?.SetDefault(true);
            var dog = await dogs.GetAsync(dogId);
            dog?.SetPhotoUrl(next?.Url);
            await UnitOfWork.SaveChangesAsync();
        }
        return url;
    }

    // --- Medical records ---

    public async Task<List<MedicalRecordDto>> GetMedicalRecordsAsync(int dogId)
        => (await dogs.GetMedicalRecordsAsync(dogId)).Select(r => r.ToDto(includeDog: false)).ToList();

    public async Task<MedicalRecordDto?> GetMedicalRecordAsync(int id)
        => (await dogs.GetMedicalRecordAsync(id))?.ToDto(includeDog: false);

    public async Task<MedicalRecordDto?> AddMedicalRecordAsync(CreateMedicalRecordRequest request)
    {
        if (!await dogs.ExistsAsync(request.DogId)) return null;
        var record = MedicalRecord.Create(
            request.DogId, request.VetName, request.Diagnosis, request.Treatment,
            request.Notes, request.NextVisitDate);
        dogs.AddMedicalRecord(record);
        await UnitOfWork.SaveChangesAsync();
        return record.ToDto(includeDog: false);
    }

    public async Task<MedicalRecordDto?> UpdateMedicalRecordAsync(UpdateMedicalRecordRequest request)
    {
        var record = await dogs.GetMedicalRecordAsync(request.Id);
        if (record is null) return null;
        record.Update(request.VetName, request.Diagnosis, request.Treatment,
            request.Notes, request.VisitDate, request.NextVisitDate);
        await UnitOfWork.SaveChangesAsync();
        return record.ToDto(includeDog: false);
    }

    public Task<bool> DeleteMedicalRecordAsync(int id)
        => SoftDeleteAsync(() => dogs.GetMedicalRecordAsync(id), dogs.RemoveMedicalRecord);

    /// <summary>A medical record may only be restored while its parent dog is still alive.</summary>
    public async Task<bool> RestoreMedicalRecordAsync(int id)
    {
        var record = await dogs.GetDeletedMedicalRecordByIdAsync(id);
        if (record is null || !await dogs.ExistsAsync(record.DogId)) return false;
        record.Restore();
        await UnitOfWork.SaveChangesAsync();
        return true;
    }

    public Task<bool> PurgeMedicalRecordAsync(int id)
        => PurgeAsync(() => dogs.GetDeletedMedicalRecordByIdAsync(id), dogs.RemoveMedicalRecordPermanently);

    public async Task<List<MedicalRecordDto>> GetDeletedMedicalRecordsAsync()
        => (await dogs.GetDeletedMedicalRecordsAsync()).Select(r => r.ToDto()).ToList();

    public async Task<MedicalRecordDto?> GetDeletedMedicalRecordAsync(int id)
        => (await dogs.GetDeletedMedicalRecordByIdAsync(id))?.ToDto();

    // --- Medications ---

    public async Task<List<MedicationDto>> GetMedicationsAsync(int dogId)
        => (await dogs.GetMedicationsAsync(dogId)).Select(m => m.ToDto(includeDog: false)).ToList();

    public async Task<MedicationDto?> GetMedicationAsync(int id)
        => (await dogs.GetMedicationAsync(id))?.ToDto(includeDog: false);

    public async Task<MedicationDto?> AddMedicationAsync(CreateMedicationRequest request)
    {
        if (!await dogs.ExistsAsync(request.DogId)) return null;
        var medication = Medication.Create(
            request.DogId, request.Name, request.Dosage, request.Frequency,
            request.StartDate, request.EndDate);
        dogs.AddMedication(medication);
        await UnitOfWork.SaveChangesAsync();
        return medication.ToDto(includeDog: false);
    }

    public async Task<MedicationDto?> UpdateMedicationAsync(UpdateMedicationRequest request)
    {
        var medication = await dogs.GetMedicationAsync(request.Id);
        if (medication is null) return null;
        medication.Update(request.Name, request.Dosage, request.Frequency,
            request.StartDate, request.EndDate, request.IsActive);
        await UnitOfWork.SaveChangesAsync();
        return medication.ToDto(includeDog: false);
    }

    public async Task<bool> DeactivateMedicationAsync(int id)
    {
        var medication = await dogs.GetMedicationAsync(id);
        if (medication is null) return false;
        medication.Deactivate();
        await UnitOfWork.SaveChangesAsync();
        return true;
    }

    public Task<bool> DeleteMedicationAsync(int id)
        => SoftDeleteAsync(() => dogs.GetMedicationAsync(id), dogs.RemoveMedication);

    /// <summary>Same parent-dog liveness rule as medical records.</summary>
    public async Task<bool> RestoreMedicationAsync(int id)
    {
        var medication = await dogs.GetDeletedMedicationByIdAsync(id);
        if (medication is null || !await dogs.ExistsAsync(medication.DogId)) return false;
        medication.Restore();
        await UnitOfWork.SaveChangesAsync();
        return true;
    }

    public Task<bool> PurgeMedicationAsync(int id)
        => PurgeAsync(() => dogs.GetDeletedMedicationByIdAsync(id), dogs.RemoveMedicationPermanently);

    public async Task<List<MedicationDto>> GetDeletedMedicationsAsync()
        => (await dogs.GetDeletedMedicationsAsync()).Select(m => m.ToDto()).ToList();

    public async Task<MedicationDto?> GetDeletedMedicationAsync(int id)
        => (await dogs.GetDeletedMedicationByIdAsync(id))?.ToDto();
}
