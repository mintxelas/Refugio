using Akka.Actor;
using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Actors;

public class AdoptionActorTests : ActorTestBase
{
    private readonly IActorRef _actor;

    public AdoptionActorTests()
    {
        _actor = Sys.ActorOf(Props.Create(() => new AdoptionActor(_sf)));
    }

    private async Task<Dog> SeedDog(string name = "TestDog")
        => await SeedAsync(db => { var d = new Dog { Name = name, Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; });

    private async Task<Adoption> SeedAdoption(int dogId, AdoptionStatus status = AdoptionStatus.Applied)
        => await SeedAsync(db =>
        {
            var a = new Adoption { DogId = dogId, ApplicantName = "Alice", ApplicantEmail = "alice@test.com", Status = status };
            db.Adoptions.Add(a);
            return a;
        });

    [Fact]
    public async Task GetAllAdoptions_ReturnsEmpty_WhenNoneExist()
    {
        var result = await _actor.Ask<List<Adoption>>(new GetAllAdoptions(), TimeSpan.FromSeconds(5));
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAdoptions_ReturnsAll()
    {
        var dog = await SeedDog();
        await SeedAdoption(dog.Id, AdoptionStatus.Applied);
        await SeedAdoption(dog.Id, AdoptionStatus.Interview);
        var result = await _actor.Ask<List<Adoption>>(new GetAllAdoptions(), TimeSpan.FromSeconds(5));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAdoptions_FiltersByStatus()
    {
        var dog = await SeedDog();
        await SeedAdoption(dog.Id, AdoptionStatus.Applied);
        await SeedAdoption(dog.Id, AdoptionStatus.Approved);
        var result = await _actor.Ask<List<Adoption>>(new GetAllAdoptions(AdoptionStatus.Applied), TimeSpan.FromSeconds(5));
        Assert.Single(result);
        Assert.Equal(AdoptionStatus.Applied, result[0].Status);
    }

    [Fact]
    public async Task GetAdoptionById_ReturnsDog_WhenFound()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id);
        var result = await _actor.Ask<Adoption?>(new GetAdoptionById(adoption.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("Alice", result.ApplicantName);
    }

    [Fact]
    public async Task CreateAdoption_CreatesAndReturns()
    {
        var dog = await SeedDog();
        var result = await _actor.Ask<Adoption>(
            new CreateAdoption(dog.Id, "Bob", "bob@test.com", "555-1234", AdoptionType.Adoption, "great home"),
            TimeSpan.FromSeconds(5));
        Assert.Equal("Bob", result.ApplicantName);
        Assert.Equal(dog.Id, result.DogId);
        Assert.Equal(AdoptionStatus.Applied, result.Status);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateAdoption_FosterType()
    {
        var dog = await SeedDog();
        var result = await _actor.Ask<Adoption>(
            new CreateAdoption(dog.Id, "Carol", null, null, AdoptionType.Foster, null),
            TimeSpan.FromSeconds(5));
        Assert.Equal(AdoptionType.Foster, result.Type);
    }

    [Fact]
    public async Task UpdateAdoptionStatus_UpdatesStatus_WhenFound()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id, AdoptionStatus.Applied);
        var result = await _actor.Ask<Adoption?>(
            new UpdateAdoptionStatus(adoption.Id, AdoptionStatus.Approved, null),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal(AdoptionStatus.Approved, result.Status);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAdoptionStatus_UpdatesNotes_WhenProvided()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id);
        var result = await _actor.Ask<Adoption?>(
            new UpdateAdoptionStatus(adoption.Id, AdoptionStatus.Interview, "home check scheduled"),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("home check scheduled", result.Notes);
    }

    [Fact]
    public async Task DeleteAdoption_ReturnsTrue_WhenFound()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id);
        var result = await _actor.Ask<bool>(new DeleteAdoption(adoption.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAdoption_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new DeleteAdoption(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAdoptions_IncludesDogNavigation()
    {
        var dog = await SeedDog("Fido");
        await SeedAdoption(dog.Id);
        var result = await _actor.Ask<List<Adoption>>(new GetAllAdoptions(), TimeSpan.FromSeconds(5));
        Assert.Single(result);
        Assert.NotNull(result[0].Dog);
        Assert.Equal("Fido", result[0].Dog.Name);
    }

    // ── GetDeletedAdoptions / RestoreAdoption ──────────────────

    [Fact]
    public async Task GetDeletedAdoptions_ReturnsEmpty_WhenNoneDeleted()
    {
        var dog = await SeedDog("LiveDog");
        await SeedAdoption(dog.Id);
        var deleted = await _actor.Ask<List<Adoption>>(new GetDeletedAdoptions(), TimeSpan.FromSeconds(5));
        Assert.Empty(deleted);
    }

    [Fact]
    public async Task GetDeletedAdoptions_ReturnsOnlyDeleted()
    {
        var dog = await SeedDog("MixedDog");
        await SeedAdoption(dog.Id, AdoptionStatus.Applied);
        await SeedAsync(db =>
        {
            var a = new Adoption { DogId = dog.Id, ApplicantName = "Gone", ApplicantEmail = "gone@test.com", Status = AdoptionStatus.Applied, DeletedAt = DateTime.UtcNow };
            db.Adoptions.Add(a);
            return a;
        });
        var deleted = await _actor.Ask<List<Adoption>>(new GetDeletedAdoptions(), TimeSpan.FromSeconds(5));
        Assert.Single(deleted);
        Assert.Equal("Gone", deleted[0].ApplicantName);
    }

    [Fact]
    public async Task RestoreAdoption_ReturnsTrue_AndAppearsInActiveQuery()
    {
        var dog = await SeedDog("ReviveDog");
        var seeded = await SeedAsync(db =>
        {
            var a = new Adoption { DogId = dog.Id, ApplicantName = "Revive", ApplicantEmail = "revive@test.com", Status = AdoptionStatus.Applied, DeletedAt = DateTime.UtcNow };
            db.Adoptions.Add(a);
            return a;
        });
        var result = await _actor.Ask<bool>(new RestoreAdoption(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
        var adoption = await _actor.Ask<Adoption?>(new GetAdoptionById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(adoption);
        Assert.Null(adoption.DeletedAt);
    }

    [Fact]
    public async Task RestoreAdoption_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new RestoreAdoption(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    // ── GetAdoptionConversionStats ─────────────────────────────

    [Fact]
    public async Task GetAdoptionConversionStats_ReturnsAllTwelveMonths()
    {
        var result = await _actor.Ask<AdoptionConversionStats>(new GetAdoptionConversionStats(2025), TimeSpan.FromSeconds(5));
        Assert.Equal(12, result.Monthly.Count);
        Assert.Equal(0, result.TotalApplied);
        Assert.Equal(0, result.TotalFinalized);
    }

    [Fact]
    public async Task GetAdoptionConversionStats_CountsAppliedAndFinalized()
    {
        var dog = await SeedDog("StatDog");
        var year = DateTime.UtcNow.Year;

        await SeedAsync(db =>
        {
            db.Adoptions.Add(new Adoption { DogId = dog.Id, ApplicantName = "A1", Status = AdoptionStatus.Applied, CreatedAt = new DateTime(year, 3, 1) });
            db.Adoptions.Add(new Adoption { DogId = dog.Id, ApplicantName = "A2", Status = AdoptionStatus.Finalized, CreatedAt = new DateTime(year, 3, 1), UpdatedAt = new DateTime(year, 3, 15) });
            return db;
        });

        var result = await _actor.Ask<AdoptionConversionStats>(new GetAdoptionConversionStats(year), TimeSpan.FromSeconds(5));
        Assert.Equal(2, result.TotalApplied);
        Assert.Equal(1, result.TotalFinalized);
        var march = result.Monthly.First(m => m.Month == 3);
        Assert.Equal(2, march.Applied);
        Assert.Equal(1, march.Finalized);
    }

    // ── GetShelterStayStats ────────────────────────────────────

    [Fact]
    public async Task GetShelterStayStats_ReturnsEmpty_WhenNoFinalizedAdoptions()
    {
        var dog = await SeedDog("StayDog");
        await SeedAdoption(dog.Id, AdoptionStatus.Applied);
        var result = await _actor.Ask<ShelterStayStats>(new GetShelterStayStats(), TimeSpan.FromSeconds(5));
        Assert.Empty(result.ByBreed);
    }

    [Fact]
    public async Task GetShelterStayStats_ComputesAvgStayByBreed()
    {
        var arrival = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var finalized = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var dog = await SeedAsync(db =>
        {
            var d = new Dog { Name = "LabStay", Breed = "Labrador", Gender = "M", ArrivalDate = arrival };
            db.Dogs.Add(d);
            return d;
        });
        await SeedAsync(db =>
        {
            var a = new Adoption { DogId = dog.Id, ApplicantName = "Adopter", Status = AdoptionStatus.Finalized, UpdatedAt = finalized };
            db.Adoptions.Add(a);
            return a;
        });

        var result = await _actor.Ask<ShelterStayStats>(new GetShelterStayStats(), TimeSpan.FromSeconds(5));
        Assert.Single(result.ByBreed);
        var labData = result.ByBreed[0];
        Assert.Equal("Labrador", labData.Breed);
        Assert.Equal(1, labData.Count);
        Assert.True(labData.AvgDays > 0);
    }
}
