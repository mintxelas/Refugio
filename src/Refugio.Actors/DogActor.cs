using Microsoft.Extensions.DependencyInjection;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Services;

namespace Refugio.Actors;

/// <summary>Routes all Dog-aggregate operations (dogs, photos, medical records, medications) to IDogService.</summary>
public sealed class DogActor : ShelterActorBase<IDogService>
{
    public DogActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        // Dogs
        Query<GetDogs>(async (s, m) => await s.GetDogsAsync(m.Search, m.Status));
        Query<GetDogsPaged>(async (s, m) => await s.GetDogsPagedAsync(m.Search, m.Status, m.Page, m.PageSize));
        Query<GetDog>(async (s, m) => await s.GetDogAsync(m.Id));
        Query<GetDeletedDogs>(async (s, _) => await s.GetDeletedDogsAsync());
        Query<GetDeletedDog>(async (s, m) => await s.GetDeletedDogAsync(m.Id));
        Command<CreateDogRequest>(async (s, m) => await s.CheckInDogAsync(m));
        Command<UpdateDogRequest>(async (s, m) => await s.UpdateDogAsync(m));
        Command<DeleteDog>(async (s, m) => await s.DeleteDogAsync(m.Id));
        Command<RestoreDog>(async (s, m) => await s.RestoreDogAsync(m.Id));
        Command<PurgeDog>(async (s, m) => await s.PurgeDogAsync(m.Id));

        // Photo gallery
        Command<SetDogPhoto>(async (s, m) => await s.SetDogPhotoAsync(m.Id, m.PhotoUrl));
        Query<GetDogPhotos>(async (s, m) => await s.GetDogPhotosAsync(m.DogId));
        Command<AddDogPhoto>(async (s, m) => await s.AddDogPhotoAsync(m.DogId, m.Url));
        Command<SetDefaultDogPhoto>(async (s, m) => await s.SetDefaultDogPhotoAsync(m.PhotoId));
        Command<RemoveDogPhoto>(async (s, m) => await s.RemoveDogPhotoAsync(m.PhotoId));

        // Medical records
        Query<GetMedicalRecords>(async (s, m) => await s.GetMedicalRecordsAsync(m.DogId));
        Query<GetMedicalRecord>(async (s, m) => await s.GetMedicalRecordAsync(m.Id));
        Query<GetDeletedMedicalRecords>(async (s, _) => await s.GetDeletedMedicalRecordsAsync());
        Query<GetDeletedMedicalRecord>(async (s, m) => await s.GetDeletedMedicalRecordAsync(m.Id));
        Command<CreateMedicalRecordRequest>(async (s, m) => await s.AddMedicalRecordAsync(m));
        Command<UpdateMedicalRecordRequest>(async (s, m) => await s.UpdateMedicalRecordAsync(m));
        Command<DeleteMedicalRecord>(async (s, m) => await s.DeleteMedicalRecordAsync(m.Id));
        Command<RestoreMedicalRecord>(async (s, m) => await s.RestoreMedicalRecordAsync(m.Id));
        Command<PurgeMedicalRecord>(async (s, m) => await s.PurgeMedicalRecordAsync(m.Id));

        // Medications
        Query<GetMedications>(async (s, m) => await s.GetMedicationsAsync(m.DogId));
        Query<GetMedication>(async (s, m) => await s.GetMedicationAsync(m.Id));
        Query<GetDeletedMedications>(async (s, _) => await s.GetDeletedMedicationsAsync());
        Query<GetDeletedMedication>(async (s, m) => await s.GetDeletedMedicationAsync(m.Id));
        Command<CreateMedicationRequest>(async (s, m) => await s.AddMedicationAsync(m));
        Command<UpdateMedicationRequest>(async (s, m) => await s.UpdateMedicationAsync(m));
        Command<DeactivateMedication>(async (s, m) => await s.DeactivateMedicationAsync(m.Id));
        Command<DeleteMedication>(async (s, m) => await s.DeleteMedicationAsync(m.Id));
        Command<RestoreMedication>(async (s, m) => await s.RestoreMedicationAsync(m.Id));
        Command<PurgeMedication>(async (s, m) => await s.PurgeMedicationAsync(m.Id));
    }
}
