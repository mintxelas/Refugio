using Refugio.Application.Contracts;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Services;

public class DogServiceTests : ServiceTestBase
{
    private Task<T> Svc<T>(Func<IDogService, Task<T>> action) => WithServiceAsync(action);

    private Task<Dog> SeedDog(string name = "TestDog", string breed = "Lab",
        DogStatus status = DogStatus.Available, DateTime? deletedAt = null, string? photoUrl = null)
        => SeedAsync(db =>
        {
            var dog = Dog.CheckIn(name, breed, 12, "M", 10m, photoUrl: photoUrl, status: status);
            dog.DeletedAt = deletedAt;
            db.Dogs.Add(dog);
            return dog;
        });

    private Task<MedicalRecord> SeedMedicalRecord(int dogId, string vetName = "Dr. Smith", DateTime? deletedAt = null)
        => SeedAsync(db =>
        {
            var record = MedicalRecord.Create(dogId, vetName, "Cold", "Rest", null, null);
            record.DeletedAt = deletedAt;
            db.MedicalRecords.Add(record);
            return record;
        });

    private Task<Medication> SeedMedication(int dogId, string name = "Pill", DateTime? deletedAt = null)
        => SeedAsync(db =>
        {
            var medication = Medication.Create(dogId, name, "1mg", "Daily", DateTime.UtcNow, null);
            medication.DeletedAt = deletedAt;
            db.Medications.Add(medication);
            return medication;
        });

    // ── Dogs ───────────────────────────────────────────────────

    [Fact]
    public async Task GetDogs_ReturnsEmptyList_WhenNoneExist()
    {
        var dogs = await Svc(s => s.GetDogsAsync());
        Assert.Empty(dogs);
    }

    [Fact]
    public async Task GetDogs_ReturnsAll_WhenDogsExist()
    {
        await SeedDog("Rex");
        await SeedDog("Buddy", "Poodle");
        var dogs = await Svc(s => s.GetDogsAsync());
        Assert.Equal(2, dogs.Count);
    }

    [Fact]
    public async Task GetDogs_FiltersByName_Search()
    {
        await SeedDog("Rex");
        await SeedDog("Buddy", "Poodle");
        var dogs = await Svc(s => s.GetDogsAsync("Rex"));
        Assert.Single(dogs);
        Assert.Equal("Rex", dogs[0].Name);
    }

    [Fact]
    public async Task GetDogs_FiltersByBreed_Search()
    {
        await SeedDog("Rex", "Labrador");
        await SeedDog("Buddy", "Poodle");
        var dogs = await Svc(s => s.GetDogsAsync("Poodle"));
        Assert.Single(dogs);
        Assert.Equal("Buddy", dogs[0].Name);
    }

    [Fact]
    public async Task GetDogs_FiltersByStatus()
    {
        await SeedDog("Available", status: DogStatus.Available);
        await SeedDog("Adopted", status: DogStatus.Adopted);
        var dogs = await Svc(s => s.GetDogsAsync(status: DogStatus.Available));
        Assert.Single(dogs);
        Assert.Equal("Available", dogs[0].Name);
    }

    [Fact]
    public async Task GetDogsPaged_ReturnsRequestedPage_WithTotals()
    {
        for (var i = 1; i <= 7; i++) await SeedDog($"Dog{i:D2}");
        var page = await Svc(s => s.GetDogsPagedAsync(null, null, page: 2, pageSize: 3));
        Assert.Equal(7, page.TotalCount);
        Assert.Equal(3, page.Items.Count);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal("Dog04", page.Items[0].Name);
    }

    [Fact]
    public async Task GetDog_ReturnsDog_WhenFound()
    {
        var seeded = await SeedDog("Max", "Husky");
        var dog = await Svc(s => s.GetDogAsync(seeded.Id));
        Assert.NotNull(dog);
        Assert.Equal("Max", dog.Name);
    }

    [Fact]
    public async Task GetDog_IncludesMedicalHistory()
    {
        var seeded = await SeedDog("Sick");
        await SeedMedicalRecord(seeded.Id, "Dr. House");
        await SeedMedication(seeded.Id, "Aspirin");
        var dog = await Svc(s => s.GetDogAsync(seeded.Id));
        Assert.NotNull(dog);
        Assert.Single(dog.MedicalRecords!);
        Assert.Single(dog.Medications!);
    }

    [Fact]
    public async Task CheckInDog_CreatesAndReturns()
    {
        var dog = await Svc(s => s.CheckInDogAsync(new CreateDogRequest("Bella", "Beagle", 18, "Female", 10.5m, null, null, null)));
        Assert.Equal("Bella", dog.Name);
        Assert.Equal("Beagle", dog.Breed);
        Assert.True(dog.Id > 0);
        Assert.Equal(DogStatus.Available, dog.Status);
    }

    [Fact]
    public async Task CheckInDog_PersistsAllFields()
    {
        var dog = await Svc(s => s.CheckInDogAsync(
            new CreateDogRequest("Rocky", "Bulldog", 24, "Male", 20m, "http://photo.jpg", "friendly,calm", "needs exercise")));
        Assert.Equal("Rocky", dog.Name);
        Assert.Equal("Bulldog", dog.Breed);
        Assert.Equal(24, dog.AgeMonths);
        Assert.Equal("Male", dog.Gender);
        Assert.Equal(20m, dog.WeightKg);
        Assert.Equal("http://photo.jpg", dog.PhotoUrl);
        Assert.Equal("friendly,calm", dog.Traits);
        Assert.Equal("needs exercise", dog.Notes);
    }

    [Fact]
    public async Task UpdateDog_UpdatesDog_WhenFound()
    {
        var seeded = await SeedDog("Old");
        var dog = await Svc(s => s.UpdateDogAsync(
            new UpdateDogRequest(seeded.Id, "New", "Husky", 24, "M", DogStatus.Adopted, 30m, null, null, null)));
        Assert.NotNull(dog);
        Assert.Equal("New", dog.Name);
        Assert.Equal(DogStatus.Adopted, dog.Status);
        Assert.Equal(30m, dog.WeightKg);
    }

    [Fact]
    public async Task UpdateDog_ReturnsNull_WhenNotFound()
    {
        var dog = await Svc(s => s.UpdateDogAsync(
            new UpdateDogRequest(99999, "X", "Y", 1, "M", DogStatus.Available, 1m, null, null, null)));
        Assert.Null(dog);
    }

    [Fact]
    public async Task DeleteDog_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedDog("ToDelete");
        Assert.True(await Svc(s => s.DeleteDogAsync(seeded.Id)));
    }

    [Fact]
    public async Task DeleteDog_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.DeleteDogAsync(99999)));
    }

    [Fact]
    public async Task DeleteDog_SoftDeletes_ExcludedFromSubsequentQueries()
    {
        var seeded = await SeedDog("GoneGirl");
        await Svc(s => s.DeleteDogAsync(seeded.Id));
        var dogs = await Svc(s => s.GetDogsAsync());
        Assert.Empty(dogs);
        var direct = await ReadDirectAsync<Dog>(seeded.Id);
        Assert.NotNull(direct!.DeletedAt);
    }

    // ── Deleted dogs / restore / purge ─────────────────────────

    [Fact]
    public async Task GetDeletedDogs_ReturnsEmpty_WhenNoneDeleted()
    {
        await SeedDog("Alive");
        Assert.Empty(await Svc(s => s.GetDeletedDogsAsync()));
    }

    [Fact]
    public async Task GetDeletedDogs_ReturnsOnlyDeleted()
    {
        await SeedDog("Active");
        await SeedDog("Gone", deletedAt: DateTime.UtcNow);
        var deleted = await Svc(s => s.GetDeletedDogsAsync());
        Assert.Single(deleted);
        Assert.Equal("Gone", deleted[0].Name);
    }

    [Fact]
    public async Task RestoreDog_ReturnsTrue_AndAppearsInActiveQuery()
    {
        var seeded = await SeedDog("Revive", deletedAt: DateTime.UtcNow);
        Assert.True(await Svc(s => s.RestoreDogAsync(seeded.Id)));
        var dog = await Svc(s => s.GetDogAsync(seeded.Id));
        Assert.NotNull(dog);
        Assert.Null(dog.DeletedAt);
    }

    [Fact]
    public async Task RestoreDog_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.RestoreDogAsync(99999)));
    }

    [Fact]
    public async Task PurgeDog_ReturnsTrue_AndRecordGoneFromGetDeleted()
    {
        var seeded = await SeedDog("Purge", deletedAt: DateTime.UtcNow);
        Assert.True(await Svc(s => s.PurgeDogAsync(seeded.Id)));
        Assert.Empty(await Svc(s => s.GetDeletedDogsAsync()));
        Assert.Null(await ReadDirectAsync<Dog>(seeded.Id));
    }

    [Fact]
    public async Task PurgeDog_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.PurgeDogAsync(99999)));
    }

    [Fact]
    public async Task PurgeDog_ReturnsFalse_WhenRecordIsLive()
    {
        var seeded = await SeedDog("LiveDog");
        Assert.False(await Svc(s => s.PurgeDogAsync(seeded.Id)));
        Assert.NotNull(await Svc(s => s.GetDogAsync(seeded.Id)));
    }

    [Fact]
    public async Task GetDeletedDog_ReturnsDog_WhenDeleted()
    {
        var seeded = await SeedDog("ByIdDeleted", "Poodle", deletedAt: DateTime.UtcNow);
        var dog = await Svc(s => s.GetDeletedDogAsync(seeded.Id));
        Assert.NotNull(dog);
        Assert.Equal("ByIdDeleted", dog.Name);
    }

    [Fact]
    public async Task GetDeletedDog_ReturnsNull_WhenLive()
    {
        var seeded = await SeedDog("StillAlive");
        Assert.Null(await Svc(s => s.GetDeletedDogAsync(seeded.Id)));
    }

    [Fact]
    public async Task GetDeletedDog_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await Svc(s => s.GetDeletedDogAsync(99999)));
    }

    // ── Dog photo url ──────────────────────────────────────────

    [Fact]
    public async Task SetDogPhoto_ReturnsTrue_AndUpdatesUrl_WhenFound()
    {
        var seeded = await SeedDog("Photo");
        Assert.True(await Svc(s => s.SetDogPhotoAsync(seeded.Id, "http://new.jpg")));
        var dog = await Svc(s => s.GetDogAsync(seeded.Id));
        Assert.Equal("http://new.jpg", dog!.PhotoUrl);
    }

    [Fact]
    public async Task SetDogPhoto_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.SetDogPhotoAsync(99999, "http://photo.jpg")));
    }

    [Fact]
    public async Task SetDogPhoto_ClearsUrl_WhenNullPassed()
    {
        var seeded = await SeedDog("Photo2", photoUrl: "http://old.jpg");
        Assert.True(await Svc(s => s.SetDogPhotoAsync(seeded.Id, null)));
        var dog = await Svc(s => s.GetDogAsync(seeded.Id));
        Assert.Null(dog!.PhotoUrl);
    }

    // ── Medical records ────────────────────────────────────────

    [Fact]
    public async Task GetMedicalRecords_ReturnsRecordsForDog()
    {
        var dog = await SeedDog("Sick");
        await SeedMedicalRecord(dog.Id, "Dr. Smith");
        var records = await Svc(s => s.GetMedicalRecordsAsync(dog.Id));
        Assert.Single(records);
        Assert.Equal("Dr. Smith", records[0].VetName);
    }

    [Fact]
    public async Task GetMedicalRecords_ReturnsEmpty_WhenNoRecords()
    {
        var dog = await SeedDog("Healthy");
        Assert.Empty(await Svc(s => s.GetMedicalRecordsAsync(dog.Id)));
    }

    [Fact]
    public async Task AddMedicalRecord_CreatesAndReturns()
    {
        var dog = await SeedDog("Doggo");
        var record = await Svc(s => s.AddMedicalRecordAsync(
            new CreateMedicalRecordRequest(dog.Id, "Dr. Jones", "Flu", "Meds", "notes", DateTime.UtcNow.AddDays(7))));
        Assert.NotNull(record);
        Assert.Equal(dog.Id, record.DogId);
        Assert.Equal("Dr. Jones", record.VetName);
        Assert.Equal("Flu", record.Diagnosis);
        Assert.True(record.Id > 0);
    }

    [Fact]
    public async Task AddMedicalRecord_ReturnsNull_WhenDogMissing()
    {
        var record = await Svc(s => s.AddMedicalRecordAsync(
            new CreateMedicalRecordRequest(99999, "Dr. Ghost", "X", "Y", null, null)));
        Assert.Null(record);
    }

    [Fact]
    public async Task UpdateMedicalRecord_UpdatesFields_WhenFound()
    {
        var dog = await SeedDog("MedEdit");
        var seeded = await SeedMedicalRecord(dog.Id, "Dr. Old");
        var visitDate = new DateTime(2026, 1, 15);
        var record = await Svc(s => s.UpdateMedicalRecordAsync(
            new UpdateMedicalRecordRequest(seeded.Id, "Dr. New", "Updated", "Therapy", "note", visitDate, null)));
        Assert.NotNull(record);
        Assert.Equal("Dr. New", record.VetName);
        Assert.Equal("Updated", record.Diagnosis);
        Assert.Equal(visitDate, record.VisitDate);
    }

    [Fact]
    public async Task UpdateMedicalRecord_ReturnsNull_WhenNotFound()
    {
        var record = await Svc(s => s.UpdateMedicalRecordAsync(
            new UpdateMedicalRecordRequest(99999, "X", "Y", "Z", null, DateTime.UtcNow, null)));
        Assert.Null(record);
    }

    [Fact]
    public async Task DeleteMedicalRecord_SoftDeletes_ExcludedFromSubsequentQueries()
    {
        var dog = await SeedDog("D");
        var record = await SeedMedicalRecord(dog.Id, "Dr. X");
        await Svc(s => s.DeleteMedicalRecordAsync(record.Id));
        Assert.Empty(await Svc(s => s.GetMedicalRecordsAsync(dog.Id)));
    }

    [Fact]
    public async Task GetDeletedMedicalRecords_ReturnsOnlyDeleted()
    {
        var dog = await SeedDog("MedDog");
        await SeedMedicalRecord(dog.Id, "Dr.A");
        await SeedMedicalRecord(dog.Id, "Dr.B", deletedAt: DateTime.UtcNow);
        var deleted = await Svc(s => s.GetDeletedMedicalRecordsAsync());
        Assert.Single(deleted);
        Assert.Equal("Dr.B", deleted[0].VetName);
    }

    [Fact]
    public async Task GetDeletedMedicalRecords_IncludesParentDog_EvenWhenDogDeleted()
    {
        var dog = await SeedDog("DeadParent", deletedAt: DateTime.UtcNow);
        await SeedMedicalRecord(dog.Id, "Dr.Orphan", deletedAt: DateTime.UtcNow);
        var deleted = await Svc(s => s.GetDeletedMedicalRecordsAsync());
        Assert.Single(deleted);
        Assert.NotNull(deleted[0].Dog);
        Assert.NotNull(deleted[0].Dog!.DeletedAt);
    }

    [Fact]
    public async Task RestoreMedicalRecord_ReturnsTrue_WhenDogAlive()
    {
        var dog = await SeedDog("Alive");
        var record = await SeedMedicalRecord(dog.Id, deletedAt: DateTime.UtcNow);
        Assert.True(await Svc(s => s.RestoreMedicalRecordAsync(record.Id)));
        Assert.Single(await Svc(s => s.GetMedicalRecordsAsync(dog.Id)));
    }

    [Fact]
    public async Task RestoreMedicalRecord_ReturnsFalse_WhenDogDeleted()
    {
        var dog = await SeedDog("DeletedParent", deletedAt: DateTime.UtcNow);
        var record = await SeedMedicalRecord(dog.Id, deletedAt: DateTime.UtcNow);
        Assert.False(await Svc(s => s.RestoreMedicalRecordAsync(record.Id)));
        var direct = await ReadDirectAsync<MedicalRecord>(record.Id);
        Assert.NotNull(direct!.DeletedAt);
    }

    [Fact]
    public async Task RestoreMedicalRecord_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.RestoreMedicalRecordAsync(99999)));
    }

    [Fact]
    public async Task PurgeMedicalRecord_ReturnsTrue_AndGoneFromGetDeleted()
    {
        var dog = await SeedDog("PurgeMedDog");
        var record = await SeedMedicalRecord(dog.Id, "Dr.P", deletedAt: DateTime.UtcNow);
        Assert.True(await Svc(s => s.PurgeMedicalRecordAsync(record.Id)));
        Assert.Empty(await Svc(s => s.GetDeletedMedicalRecordsAsync()));
    }

    [Fact]
    public async Task PurgeMedicalRecord_ReturnsFalse_WhenLive()
    {
        var dog = await SeedDog("LiveMedDog");
        var record = await SeedMedicalRecord(dog.Id, "Dr.L");
        Assert.False(await Svc(s => s.PurgeMedicalRecordAsync(record.Id)));
    }

    // ── Medications ────────────────────────────────────────────

    [Fact]
    public async Task GetMedications_ReturnsMedications()
    {
        var dog = await SeedDog("Medicated");
        await SeedMedication(dog.Id, "Aspirin");
        var meds = await Svc(s => s.GetMedicationsAsync(dog.Id));
        Assert.Single(meds);
        Assert.Equal("Aspirin", meds[0].Name);
    }

    [Fact]
    public async Task AddMedication_CreatesAndReturns()
    {
        var dog = await SeedDog("Dog2");
        var med = await Svc(s => s.AddMedicationAsync(
            new CreateMedicationRequest(dog.Id, "Aspirin", "1mg", "Daily", DateTime.UtcNow, null)));
        Assert.NotNull(med);
        Assert.Equal("Aspirin", med.Name);
        Assert.Equal(dog.Id, med.DogId);
        Assert.True(med.IsActive);
        Assert.True(med.Id > 0);
    }

    [Fact]
    public async Task AddMedication_ReturnsNull_WhenDogMissing()
    {
        var med = await Svc(s => s.AddMedicationAsync(
            new CreateMedicationRequest(99999, "Ghost", "1mg", "Daily", DateTime.UtcNow, null)));
        Assert.Null(med);
    }

    [Fact]
    public async Task UpdateMedication_UpdatesFields_WhenFound()
    {
        var dog = await SeedDog("MedUpd");
        var seeded = await SeedMedication(dog.Id, "Old");
        var med = await Svc(s => s.UpdateMedicationAsync(
            new UpdateMedicationRequest(seeded.Id, "New", "2mg", "Weekly", DateTime.UtcNow, null, false)));
        Assert.NotNull(med);
        Assert.Equal("New", med.Name);
        Assert.Equal("2mg", med.Dosage);
        Assert.False(med.IsActive);
    }

    [Fact]
    public async Task DeactivateMedication_ReturnsTrue_WhenFound()
    {
        var dog = await SeedDog("D");
        var med = await SeedMedication(dog.Id);
        Assert.True(await Svc(s => s.DeactivateMedicationAsync(med.Id)));
        var direct = await ReadDirectAsync<Medication>(med.Id);
        Assert.False(direct!.IsActive);
    }

    [Fact]
    public async Task DeactivateMedication_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.DeactivateMedicationAsync(99999)));
    }

    [Fact]
    public async Task DeleteMedication_SoftDeletes_ExcludedFromSubsequentQueries()
    {
        var dog = await SeedDog("D2");
        var med = await SeedMedication(dog.Id);
        await Svc(s => s.DeleteMedicationAsync(med.Id));
        Assert.Empty(await Svc(s => s.GetMedicationsAsync(dog.Id)));
    }

    [Fact]
    public async Task GetDeletedMedications_ReturnsOnlyDeleted()
    {
        var dog = await SeedDog("MedsDog");
        await SeedMedication(dog.Id, "Active");
        await SeedMedication(dog.Id, "OldPill", deletedAt: DateTime.UtcNow);
        var deleted = await Svc(s => s.GetDeletedMedicationsAsync());
        Assert.Single(deleted);
        Assert.Equal("OldPill", deleted[0].Name);
    }

    [Fact]
    public async Task RestoreMedication_ReturnsTrue_WhenDogAlive()
    {
        var dog = await SeedDog("AliveMedDog");
        var med = await SeedMedication(dog.Id, deletedAt: DateTime.UtcNow);
        Assert.True(await Svc(s => s.RestoreMedicationAsync(med.Id)));
    }

    [Fact]
    public async Task RestoreMedication_ReturnsFalse_WhenDogDeleted()
    {
        var dog = await SeedDog("DeadMedDog", deletedAt: DateTime.UtcNow);
        var med = await SeedMedication(dog.Id, "OrphanPill", deletedAt: DateTime.UtcNow);
        Assert.False(await Svc(s => s.RestoreMedicationAsync(med.Id)));
    }

    [Fact]
    public async Task RestoreMedication_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.RestoreMedicationAsync(99999)));
    }

    [Fact]
    public async Task PurgeMedication_ReturnsTrue_AndGoneFromGetDeleted()
    {
        var dog = await SeedDog("PurgeMedsDog");
        var med = await SeedMedication(dog.Id, "PurgePill", deletedAt: DateTime.UtcNow);
        Assert.True(await Svc(s => s.PurgeMedicationAsync(med.Id)));
        Assert.Empty(await Svc(s => s.GetDeletedMedicationsAsync()));
    }

    // ── Photo gallery ──────────────────────────────────────────

    [Fact]
    public async Task AddDogPhoto_FirstPhoto_BecomesDefault_AndSetsDogPhotoUrl()
    {
        var dog = await SeedDog("Gallery1");
        var photo = await Svc(s => s.AddDogPhotoAsync(dog.Id, "/dogs/a.jpg"));
        Assert.NotNull(photo);
        Assert.True(photo.IsDefault);
        var refreshed = await Svc(s => s.GetDogAsync(dog.Id));
        Assert.Equal("/dogs/a.jpg", refreshed!.PhotoUrl);
    }

    [Fact]
    public async Task AddDogPhoto_SecondPhoto_NotDefault()
    {
        var dog = await SeedDog("Gallery2");
        await Svc(s => s.AddDogPhotoAsync(dog.Id, "/dogs/a.jpg"));
        var second = await Svc(s => s.AddDogPhotoAsync(dog.Id, "/dogs/b.jpg"));
        Assert.False(second!.IsDefault);
        var photos = await Svc(s => s.GetDogPhotosAsync(dog.Id));
        Assert.Equal(2, photos.Count);
        Assert.Single(photos, p => p.IsDefault);
    }

    [Fact]
    public async Task AddDogPhoto_ReturnsNull_WhenDogMissing()
    {
        Assert.Null(await Svc(s => s.AddDogPhotoAsync(99999, "/dogs/x.jpg")));
    }

    [Fact]
    public async Task SetDefaultDogPhoto_SwitchesDefault_AndUpdatesDogPhotoUrl()
    {
        var dog = await SeedDog("Gallery3");
        await Svc(s => s.AddDogPhotoAsync(dog.Id, "/dogs/a.jpg"));
        var second = await Svc(s => s.AddDogPhotoAsync(dog.Id, "/dogs/b.jpg"));

        Assert.True(await Svc(s => s.SetDefaultDogPhotoAsync(second!.Id)));

        var photos = await Svc(s => s.GetDogPhotosAsync(dog.Id));
        Assert.True(photos.Single(p => p.Id == second!.Id).IsDefault);
        Assert.Single(photos, p => p.IsDefault);
        var refreshed = await Svc(s => s.GetDogAsync(dog.Id));
        Assert.Equal("/dogs/b.jpg", refreshed!.PhotoUrl);
    }

    [Fact]
    public async Task RemoveDogPhoto_PromotesAnotherPhoto_WhenDefaultDeleted()
    {
        var dog = await SeedDog("Gallery4");
        var first = await Svc(s => s.AddDogPhotoAsync(dog.Id, "/dogs/a.jpg"));
        await Svc(s => s.AddDogPhotoAsync(dog.Id, "/dogs/b.jpg"));

        var deletedUrl = await Svc(s => s.RemoveDogPhotoAsync(first!.Id));
        Assert.Equal("/dogs/a.jpg", deletedUrl);

        var photos = await Svc(s => s.GetDogPhotosAsync(dog.Id));
        Assert.Single(photos);
        Assert.True(photos[0].IsDefault);
        var refreshed = await Svc(s => s.GetDogAsync(dog.Id));
        Assert.Equal("/dogs/b.jpg", refreshed!.PhotoUrl);
    }

    [Fact]
    public async Task RemoveDogPhoto_LastPhoto_ClearsDogPhotoUrl()
    {
        var dog = await SeedDog("Gallery5");
        var only = await Svc(s => s.AddDogPhotoAsync(dog.Id, "/dogs/a.jpg"));

        var deletedUrl = await Svc(s => s.RemoveDogPhotoAsync(only!.Id));
        Assert.Equal("/dogs/a.jpg", deletedUrl);

        Assert.Empty(await Svc(s => s.GetDogPhotosAsync(dog.Id)));
        var refreshed = await Svc(s => s.GetDogAsync(dog.Id));
        Assert.Null(refreshed!.PhotoUrl);
    }

    [Fact]
    public async Task RemoveDogPhoto_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await Svc(s => s.RemoveDogPhotoAsync(99999)));
    }
}
