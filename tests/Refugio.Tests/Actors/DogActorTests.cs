using Akka.Actor;
using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Actors;

public class DogActorTests : ActorTestBase
{
    private readonly IActorRef _actor;

    public DogActorTests()
    {
        _actor = Sys.ActorOf(Props.Create(() => new DogActor(_sf)));
    }

    [Fact]
    public async Task GetAllDogs_ReturnsEmptyList_WhenNoneExist()
    {
        var dogs = await _actor.Ask<List<Dog>>(new GetAllDogs(), TimeSpan.FromSeconds(5));
        Assert.Empty(dogs);
    }

    [Fact]
    public async Task GetAllDogs_ReturnsAll_WhenDogsExist()
    {
        await SeedAsync(db => { db.Dogs.AddRange(new Dog { Name = "Rex", Breed = "Lab", Gender = "M" }, new Dog { Name = "Buddy", Breed = "Poodle", Gender = "M" }); return db; });
        var dogs = await _actor.Ask<List<Dog>>(new GetAllDogs(), TimeSpan.FromSeconds(5));
        Assert.Equal(2, dogs.Count);
    }

    [Fact]
    public async Task GetAllDogs_FiltersByName_Search()
    {
        await SeedAsync(db => { db.Dogs.AddRange(new Dog { Name = "Rex", Breed = "Lab", Gender = "M" }, new Dog { Name = "Buddy", Breed = "Poodle", Gender = "M" }); return db; });
        var dogs = await _actor.Ask<List<Dog>>(new GetAllDogs(Search: "Rex"), TimeSpan.FromSeconds(5));
        Assert.Single(dogs);
        Assert.Equal("Rex", dogs[0].Name);
    }

    [Fact]
    public async Task GetAllDogs_FiltersByBreed_Search()
    {
        await SeedAsync(db => { db.Dogs.AddRange(new Dog { Name = "Rex", Breed = "Labrador", Gender = "M" }, new Dog { Name = "Buddy", Breed = "Poodle", Gender = "M" }); return db; });
        var dogs = await _actor.Ask<List<Dog>>(new GetAllDogs(Search: "Poodle"), TimeSpan.FromSeconds(5));
        Assert.Single(dogs);
        Assert.Equal("Buddy", dogs[0].Name);
    }

    [Fact]
    public async Task GetAllDogs_FiltersByStatus()
    {
        await SeedAsync(db =>
        {
            db.Dogs.AddRange(
                new Dog { Name = "Available", Breed = "Lab", Gender = "M", Status = DogStatus.Available },
                new Dog { Name = "Adopted", Breed = "Lab", Gender = "M", Status = DogStatus.Adopted });
            return db;
        });
        var dogs = await _actor.Ask<List<Dog>>(new GetAllDogs(Status: DogStatus.Available), TimeSpan.FromSeconds(5));
        Assert.Single(dogs);
        Assert.Equal("Available", dogs[0].Name);
    }

    [Fact]
    public async Task GetDogById_ReturnsDog_WhenFound()
    {
        var seeded = await SeedAsync(db =>
        {
            var d = new Dog { Name = "Max", Breed = "Husky", Gender = "M" };
            db.Dogs.Add(d);
            return d;
        });
        var dog = await _actor.Ask<Dog?>(new GetDogById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(dog);
        Assert.Equal("Max", dog.Name);
    }

    [Fact]
    public async Task CreateDog_CreatesAndReturns()
    {
        var dog = await _actor.Ask<Dog>(new CreateDog("Bella", "Beagle", 18, "Female", 10.5m, null, null, null), TimeSpan.FromSeconds(5));
        Assert.Equal("Bella", dog.Name);
        Assert.Equal("Beagle", dog.Breed);
        Assert.True(dog.Id > 0);
    }

    [Fact]
    public async Task CreateDog_PersistsAllFields()
    {
        var dog = await _actor.Ask<Dog>(
            new CreateDog("Rocky", "Bulldog", 24, "Male", 20m, "http://photo.jpg", "friendly,calm", "needs exercise"),
            TimeSpan.FromSeconds(5));
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
        var seeded = await SeedAsync(db =>
        {
            var d = new Dog { Name = "Old", Breed = "Lab", Gender = "M" };
            db.Dogs.Add(d);
            return d;
        });
        var dog = await _actor.Ask<Dog?>(
            new UpdateDog(seeded.Id, "New", "Husky", 24, "M", DogStatus.Adopted, 30m, null, null, null),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(dog);
        Assert.Equal("New", dog.Name);
        Assert.Equal(DogStatus.Adopted, dog.Status);
        Assert.Equal(30m, dog.WeightKg);
    }

    [Fact]
    public async Task DeleteDog_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedAsync(db =>
        {
            var d = new Dog { Name = "ToDelete", Breed = "Lab", Gender = "M" };
            db.Dogs.Add(d);
            return d;
        });
        var result = await _actor.Ask<bool>(new DeleteDog(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteDog_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new DeleteDog(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    [Fact]
    public async Task GetMedicalRecords_ReturnsRecordsForDog()
    {
        var (dog, _) = await SeedRelatedAsync(
            db => { var d = new Dog { Name = "Sick", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; },
            (db, d) => { var r = new MedicalRecord { DogId = d.Id, VetName = "Dr. Smith", Diagnosis = "Cold", Treatment = "Rest" }; db.MedicalRecords.Add(r); return r; });
        var records = await _actor.Ask<List<MedicalRecord>>(new GetMedicalRecords(dog.Id), TimeSpan.FromSeconds(5));
        Assert.Single(records);
        Assert.Equal("Dr. Smith", records[0].VetName);
    }

    [Fact]
    public async Task GetMedicalRecords_ReturnsEmpty_WhenNoRecords()
    {
        var seeded = await SeedAsync(db => { var d = new Dog { Name = "Healthy", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; });
        var records = await _actor.Ask<List<MedicalRecord>>(new GetMedicalRecords(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.Empty(records);
    }

    [Fact]
    public async Task CreateMedicalRecord_CreatesAndReturns()
    {
        var dog = await SeedAsync(db => { var d = new Dog { Name = "Doggo", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; });
        var record = await _actor.Ask<MedicalRecord>(
            new CreateMedicalRecord(dog.Id, "Dr. Jones", "Flu", "Meds", "notes", DateTime.UtcNow.AddDays(7)),
            TimeSpan.FromSeconds(5));
        Assert.Equal(dog.Id, record.DogId);
        Assert.Equal("Dr. Jones", record.VetName);
        Assert.Equal("Flu", record.Diagnosis);
        Assert.True(record.Id > 0);
    }

    [Fact]
    public async Task GetMedications_ReturnsMedications()
    {
        var (dog, _) = await SeedRelatedAsync(
            db => { var d = new Dog { Name = "Medicated", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; },
            (db, d) => { var m = new Medication { DogId = d.Id, Name = "Aspirin", Dosage = "1mg", Frequency = "Daily" }; db.Medications.Add(m); return m; });
        var meds = await _actor.Ask<List<Medication>>(new GetMedications(dog.Id), TimeSpan.FromSeconds(5));
        Assert.Single(meds);
        Assert.Equal("Aspirin", meds[0].Name);
    }

    [Fact]
    public async Task CreateMedication_CreatesAndReturns()
    {
        var dog = await SeedAsync(db => { var d = new Dog { Name = "Dog2", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; });
        var start = DateTime.UtcNow;
        var med = await _actor.Ask<Medication>(
            new CreateMedication(dog.Id, "Aspirin", "1mg", "Daily", start, null),
            TimeSpan.FromSeconds(5));
        Assert.Equal("Aspirin", med.Name);
        Assert.Equal(dog.Id, med.DogId);
        Assert.True(med.IsActive);
        Assert.True(med.Id > 0);
    }

    [Fact]
    public async Task DeactivateMedication_ReturnsTrue_WhenFound()
    {
        var (_, med) = await SeedRelatedAsync(
            db => { var d = new Dog { Name = "D", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; },
            (db, d) => { var m = new Medication { DogId = d.Id, Name = "Pill", Dosage = "1mg", Frequency = "Daily", IsActive = true }; db.Medications.Add(m); return m; });
        var result = await _actor.Ask<bool>(new DeactivateMedication(med.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task DeactivateMedication_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new DeactivateMedication(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    [Fact]
    public async Task GetDashboardStats_ReturnsStatsWithCorrectGoal()
    {
        var stats = await _actor.Ask<DashboardStats>(new GetDashboardStats(), TimeSpan.FromSeconds(5));
        Assert.NotNull(stats);
        Assert.Equal(12000m, stats.DonationGoal);
        Assert.Equal(0, stats.TotalDogs);
    }

    [Fact]
    public async Task GetDashboardStats_CountsDogs()
    {
        await SeedAsync(db => { db.Dogs.Add(new Dog { Name = "A", Breed = "Lab", Gender = "M" }); return db; });
        var stats = await _actor.Ask<DashboardStats>(new GetDashboardStats(), TimeSpan.FromSeconds(5));
        Assert.Equal(1, stats.TotalDogs);
    }

    [Fact]
    public async Task UpdateDogPhoto_ReturnsTrue_AndUpdatesUrl_WhenFound()
    {
        var seeded = await SeedAsync(db =>
        {
            var d = new Dog { Name = "Photo", Breed = "Lab", Gender = "M" };
            db.Dogs.Add(d);
            return d;
        });
        var result = await _actor.Ask<bool>(new UpdateDogPhoto(seeded.Id, "http://new.jpg"), TimeSpan.FromSeconds(5));
        Assert.True(result);
        var dog = await _actor.Ask<Dog?>(new GetDogById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.Equal("http://new.jpg", dog!.PhotoUrl);
    }

    [Fact]
    public async Task UpdateDogPhoto_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new UpdateDogPhoto(99999, "http://photo.jpg"), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateDogPhoto_ClearsUrl_WhenNullPassed()
    {
        var seeded = await SeedAsync(db =>
        {
            var d = new Dog { Name = "Photo2", Breed = "Lab", Gender = "M", PhotoUrl = "http://old.jpg" };
            db.Dogs.Add(d);
            return d;
        });
        var result = await _actor.Ask<bool>(new UpdateDogPhoto(seeded.Id, null), TimeSpan.FromSeconds(5));
        Assert.True(result);
        var dog = await _actor.Ask<Dog?>(new GetDogById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.Null(dog!.PhotoUrl);
    }

    [Fact]
    public async Task DeleteDog_SoftDeletes_ExcludedFromSubsequentQueries()
    {
        var seeded = await SeedAsync(db =>
        {
            var d = new Dog { Name = "GoneGirl", Breed = "Lab", Gender = "F" };
            db.Dogs.Add(d);
            return d;
        });
        await _actor.Ask<bool>(new DeleteDog(seeded.Id), TimeSpan.FromSeconds(5));
        var dogs = await _actor.Ask<List<Dog>>(new GetAllDogs(), TimeSpan.FromSeconds(5));
        Assert.Empty(dogs);
    }

    [Fact]
    public async Task DeleteMedicalRecord_SoftDeletes_ExcludedFromSubsequentQueries()
    {
        var (dog, record) = await SeedRelatedAsync(
            db => { var d = new Dog { Name = "D", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; },
            (db, d) => { var r = new MedicalRecord { DogId = d.Id, VetName = "Dr. X", Diagnosis = "Cold", Treatment = "Rest" }; db.MedicalRecords.Add(r); return r; });
        await _actor.Ask<bool>(new DeleteMedicalRecord(record.Id), TimeSpan.FromSeconds(5));
        var records = await _actor.Ask<List<MedicalRecord>>(new GetMedicalRecords(dog.Id), TimeSpan.FromSeconds(5));
        Assert.Empty(records);
    }

    [Fact]
    public async Task DeleteMedication_SoftDeletes_ExcludedFromSubsequentQueries()
    {
        var (dog, med) = await SeedRelatedAsync(
            db => { var d = new Dog { Name = "D2", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; },
            (db, d) => { var m = new Medication { DogId = d.Id, Name = "Pill", Dosage = "1mg", Frequency = "Daily", IsActive = true }; db.Medications.Add(m); return m; });
        await _actor.Ask<bool>(new DeleteMedication(med.Id), TimeSpan.FromSeconds(5));
        var meds = await _actor.Ask<List<Medication>>(new GetMedications(dog.Id), TimeSpan.FromSeconds(5));
        Assert.Empty(meds);
    }

    // ── GetDeletedDogs / RestoreDog ────────────────────────────

    [Fact]
    public async Task GetDeletedDogs_ReturnsEmpty_WhenNoneDeleted()
    {
        await SeedAsync(db => { db.Dogs.Add(new Dog { Name = "Alive", Breed = "Lab", Gender = "M" }); return db; });
        var deleted = await _actor.Ask<List<Dog>>(new GetDeletedDogs(), TimeSpan.FromSeconds(5));
        Assert.Empty(deleted);
    }

    [Fact]
    public async Task GetDeletedDogs_ReturnsOnlyDeleted()
    {
        await SeedAsync(db =>
        {
            db.Dogs.Add(new Dog { Name = "Active", Breed = "Lab", Gender = "M" });
            db.Dogs.Add(new Dog { Name = "Gone", Breed = "Lab", Gender = "M", DeletedAt = DateTime.UtcNow });
            return db;
        });
        var deleted = await _actor.Ask<List<Dog>>(new GetDeletedDogs(), TimeSpan.FromSeconds(5));
        Assert.Single(deleted);
        Assert.Equal("Gone", deleted[0].Name);
    }

    [Fact]
    public async Task RestoreDog_ReturnsTrue_AndAppearsInActiveQuery()
    {
        var seeded = await SeedAsync(db =>
        {
            var d = new Dog { Name = "Revive", Breed = "Lab", Gender = "M", DeletedAt = DateTime.UtcNow };
            db.Dogs.Add(d);
            return d;
        });
        var result = await _actor.Ask<bool>(new RestoreDog(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
        var dog = await _actor.Ask<Dog?>(new GetDogById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(dog);
        Assert.Null(dog.DeletedAt);
    }

    [Fact]
    public async Task RestoreDog_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new RestoreDog(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    // ── GetDeletedMedicalRecords / RestoreMedicalRecord ────────

    [Fact]
    public async Task GetDeletedMedicalRecords_ReturnsOnlyDeleted()
    {
        var (dog, _) = await SeedRelatedAsync(
            db => { var d = new Dog { Name = "MedDog", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; },
            (db, d) =>
            {
                db.MedicalRecords.Add(new MedicalRecord { DogId = d.Id, VetName = "Dr.A", Diagnosis = "Fine", Treatment = "None" });
                var deleted = new MedicalRecord { DogId = d.Id, VetName = "Dr.B", Diagnosis = "Old", Treatment = "Done", DeletedAt = DateTime.UtcNow };
                db.MedicalRecords.Add(deleted);
                return deleted;
            });
        var deleted = await _actor.Ask<List<MedicalRecord>>(new GetDeletedMedicalRecords(), TimeSpan.FromSeconds(5));
        Assert.Single(deleted);
        Assert.Equal("Dr.B", deleted[0].VetName);
    }

    [Fact]
    public async Task RestoreMedicalRecord_ReturnsTrue_WhenDogAlive()
    {
        var (_, record) = await SeedRelatedAsync(
            db => { var d = new Dog { Name = "Alive", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; },
            (db, d) =>
            {
                var r = new MedicalRecord { DogId = d.Id, VetName = "Dr.X", Diagnosis = "Cold", Treatment = "Rest", DeletedAt = DateTime.UtcNow };
                db.MedicalRecords.Add(r);
                return r;
            });
        var result = await _actor.Ask<bool>(new RestoreMedicalRecord(record.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task RestoreMedicalRecord_ReturnsFalse_WhenDogDeleted()
    {
        var (_, record) = await SeedRelatedAsync(
            db =>
            {
                var d = new Dog { Name = "DeletedParent", Breed = "Lab", Gender = "M", DeletedAt = DateTime.UtcNow };
                db.Dogs.Add(d);
                return d;
            },
            (db, d) =>
            {
                var r = new MedicalRecord { DogId = d.Id, VetName = "Dr.Y", Diagnosis = "Flu", Treatment = "Meds", DeletedAt = DateTime.UtcNow };
                db.MedicalRecords.Add(r);
                return r;
            });
        var result = await _actor.Ask<bool>(new RestoreMedicalRecord(record.Id), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    [Fact]
    public async Task RestoreMedicalRecord_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new RestoreMedicalRecord(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    // ── GetDeletedMedications / RestoreMedication ──────────────

    [Fact]
    public async Task GetDeletedMedications_ReturnsOnlyDeleted()
    {
        var _ = await SeedRelatedAsync(
            db => { var d = new Dog { Name = "MedsDog", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; },
            (db, d) =>
            {
                db.Medications.Add(new Medication { DogId = d.Id, Name = "Active", Dosage = "1mg", Frequency = "Daily" });
                var deleted = new Medication { DogId = d.Id, Name = "OldPill", Dosage = "2mg", Frequency = "Weekly", DeletedAt = DateTime.UtcNow };
                db.Medications.Add(deleted);
                return deleted;
            });
        var deleted = await _actor.Ask<List<Medication>>(new GetDeletedMedications(), TimeSpan.FromSeconds(5));
        Assert.Single(deleted);
        Assert.Equal("OldPill", deleted[0].Name);
    }

    [Fact]
    public async Task RestoreMedication_ReturnsTrue_WhenDogAlive()
    {
        var (_, med) = await SeedRelatedAsync(
            db => { var d = new Dog { Name = "AliveMedDog", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; },
            (db, d) =>
            {
                var m = new Medication { DogId = d.Id, Name = "Pill", Dosage = "1mg", Frequency = "Daily", DeletedAt = DateTime.UtcNow };
                db.Medications.Add(m);
                return m;
            });
        var result = await _actor.Ask<bool>(new RestoreMedication(med.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task RestoreMedication_ReturnsFalse_WhenDogDeleted()
    {
        var (_, med) = await SeedRelatedAsync(
            db =>
            {
                var d = new Dog { Name = "DeadMedDog", Breed = "Lab", Gender = "M", DeletedAt = DateTime.UtcNow };
                db.Dogs.Add(d);
                return d;
            },
            (db, d) =>
            {
                var m = new Medication { DogId = d.Id, Name = "OrphanPill", Dosage = "1mg", Frequency = "Daily", DeletedAt = DateTime.UtcNow };
                db.Medications.Add(m);
                return m;
            });
        var result = await _actor.Ask<bool>(new RestoreMedication(med.Id), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    [Fact]
    public async Task RestoreMedication_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new RestoreMedication(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }
}
