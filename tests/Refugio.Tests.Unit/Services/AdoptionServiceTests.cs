using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Abstractions;
using Refugio.Application.Contracts;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Services;

public class AdoptionServiceTests : ServiceTestBase
{
    private Task<T> Svc<T>(Func<IAdoptionService, Task<T>> action) => WithServiceAsync(action);

    private Task<Dog> SeedDog(string name = "TestDog")
        => SeedAsync(db => { var d = Dog.CheckIn(name, "Lab", 12, "M", 10m); db.Dogs.Add(d); return d; });

    private Task<Adoption> SeedAdoption(int dogId, AdoptionStatus status = AdoptionStatus.Applied,
        string applicant = "Alice", string? email = "alice@test.com", DateTime? deletedAt = null)
        => SeedAsync(db =>
        {
            var adoption = Adoption.Submit(dogId, applicant, email, null, AdoptionType.Adoption, null, status);
            adoption.DeletedAt = deletedAt;
            db.Adoptions.Add(adoption);
            return adoption;
        });

    [Fact]
    public async Task GetAdoptions_ReturnsEmpty_WhenNoneExist()
    {
        Assert.Empty(await Svc(s => s.GetAdoptionsAsync()));
    }

    [Fact]
    public async Task GetAdoptions_ReturnsAll()
    {
        var dog = await SeedDog();
        await SeedAdoption(dog.Id, AdoptionStatus.Applied);
        await SeedAdoption(dog.Id, AdoptionStatus.Interview);
        Assert.Equal(2, (await Svc(s => s.GetAdoptionsAsync())).Count);
    }

    [Fact]
    public async Task GetAdoptions_FiltersByStatus()
    {
        var dog = await SeedDog();
        await SeedAdoption(dog.Id, AdoptionStatus.Applied);
        await SeedAdoption(dog.Id, AdoptionStatus.Approved);
        var result = await Svc(s => s.GetAdoptionsAsync(AdoptionStatus.Applied));
        Assert.Single(result);
        Assert.Equal(AdoptionStatus.Applied, result[0].Status);
    }

    [Fact]
    public async Task GetAdoptions_IncludesDogNavigation()
    {
        var dog = await SeedDog("Fido");
        await SeedAdoption(dog.Id);
        var result = await Svc(s => s.GetAdoptionsAsync());
        Assert.Single(result);
        Assert.NotNull(result[0].Dog);
        Assert.Equal("Fido", result[0].Dog!.Name);
    }

    [Fact]
    public async Task GetAdoptionsPaged_CapsItems_AndReportsTotal()
    {
        var dog = await SeedDog();
        for (var i = 0; i < 7; i++) await SeedAdoption(dog.Id, applicant: $"App{i}");
        var page = await Svc(s => s.GetAdoptionsPagedAsync(AdoptionStatus.Applied, 1, 5));
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(7, page.TotalCount);
    }

    [Fact]
    public async Task GetAdoption_ReturnsAdoption_WhenFound()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id);
        var result = await Svc(s => s.GetAdoptionAsync(adoption.Id));
        Assert.NotNull(result);
        Assert.Equal("Alice", result.ApplicantName);
    }

    [Fact]
    public async Task Submit_CreatesAndReturns()
    {
        var dog = await SeedDog();
        var result = await Svc(s => s.SubmitAsync(
            new CreateAdoptionRequest(dog.Id, "Bob", "bob@test.com", "555-1234", AdoptionType.Adoption, "great home")));
        Assert.Equal("Bob", result.ApplicantName);
        Assert.Equal(dog.Id, result.DogId);
        Assert.Equal(AdoptionStatus.Applied, result.Status);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task Submit_FosterType()
    {
        var dog = await SeedDog();
        var result = await Svc(s => s.SubmitAsync(
            new CreateAdoptionRequest(dog.Id, "Carol", null, null, AdoptionType.Foster, null)));
        Assert.Equal(AdoptionType.Foster, result.Type);
    }

    [Fact]
    public async Task Submit_WithDatesAndFees_PersistsAllFourFields()
    {
        var dog = await SeedDog();
        var preDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var adoptDate = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        var result = await Svc(s => s.SubmitAsync(new CreateAdoptionRequest(
            dog.Id, "Dana", null, null, AdoptionType.Adoption, null,
            PreAdoptionDate: preDate, AdoptionDate: adoptDate,
            PreAdoptionFeeCharged: true, AdoptionFeeCharged: true)));
        Assert.Equal(preDate, result.PreAdoptionDate);
        Assert.Equal(adoptDate, result.AdoptionDate);
        Assert.True(result.PreAdoptionFeeCharged);
        Assert.True(result.AdoptionFeeCharged);
    }

    [Fact]
    public async Task Submit_DefaultDatesAndFees_AreNullAndFalse()
    {
        var dog = await SeedDog();
        var result = await Svc(s => s.SubmitAsync(
            new CreateAdoptionRequest(dog.Id, "Eve", null, null, AdoptionType.Adoption, null)));
        Assert.Null(result.PreAdoptionDate);
        Assert.Null(result.AdoptionDate);
        Assert.False(result.PreAdoptionFeeCharged);
        Assert.False(result.AdoptionFeeCharged);
    }

    [Fact]
    public async Task Update_PersistsDatesAndFees()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id);
        var preDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var adoptDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = await Svc(s => s.UpdateAsync(new UpdateAdoptionRequest(
            adoption.Id, "Alice", "alice@test.com", null, AdoptionType.Adoption, AdoptionStatus.Applied, null,
            PreAdoptionDate: preDate, AdoptionDate: adoptDate,
            PreAdoptionFeeCharged: false, AdoptionFeeCharged: true)));
        Assert.NotNull(result);
        Assert.Equal(preDate, result.PreAdoptionDate);
        Assert.Equal(adoptDate, result.AdoptionDate);
        Assert.False(result.PreAdoptionFeeCharged);
        Assert.True(result.AdoptionFeeCharged);
    }

    [Fact]
    public async Task Update_ClearsDates_WhenSetToNull()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id);
        var preDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        await Svc(s => s.UpdateAsync(new UpdateAdoptionRequest(
            adoption.Id, "Alice", null, null, AdoptionType.Adoption, AdoptionStatus.Applied, null,
            PreAdoptionDate: preDate)));
        var cleared = await Svc(s => s.UpdateAsync(new UpdateAdoptionRequest(
            adoption.Id, "Alice", null, null, AdoptionType.Adoption, AdoptionStatus.Applied, null,
            PreAdoptionDate: null)));
        Assert.NotNull(cleared);
        Assert.Null(cleared.PreAdoptionDate);
    }

    [Fact]
    public async Task ChangeStatus_UpdatesStatus_WhenFound()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id, AdoptionStatus.Applied);
        var result = await Svc(s => s.ChangeStatusAsync(
            new UpdateAdoptionStatusRequest(adoption.Id, AdoptionStatus.Approved, null)));
        Assert.NotNull(result);
        Assert.Equal(AdoptionStatus.Approved, result.Status);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task ChangeStatus_UpdatesNotes_WhenProvided()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id);
        var result = await Svc(s => s.ChangeStatusAsync(
            new UpdateAdoptionStatusRequest(adoption.Id, AdoptionStatus.Interview, "home check scheduled")));
        Assert.NotNull(result);
        Assert.Equal("home check scheduled", result.Notes);
    }

    [Fact]
    public async Task ChangeStatus_ReturnsNull_WhenNotFound()
    {
        var result = await Svc(s => s.ChangeStatusAsync(
            new UpdateAdoptionStatusRequest(99999, AdoptionStatus.Approved, null)));
        Assert.Null(result);
    }

    [Fact]
    public async Task Update_EditsAllFields_WhenFound()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id);
        var result = await Svc(s => s.UpdateAsync(new UpdateAdoptionRequest(
            adoption.Id, "Renamed", "new@test.com", "555-0000", AdoptionType.Foster, AdoptionStatus.HomeCheck, "edited")));
        Assert.NotNull(result);
        Assert.Equal("Renamed", result.ApplicantName);
        Assert.Equal(AdoptionType.Foster, result.Type);
        Assert.Equal(AdoptionStatus.HomeCheck, result.Status);
        Assert.Equal("edited", result.Notes);
    }

    [Fact]
    public async Task Advance_MovesThroughPipeline_AndStopsAtFinalized()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id, AdoptionStatus.Applied);

        var afterFirst = await Svc(s => s.AdvanceAsync(adoption.Id));
        Assert.Equal(AdoptionStatus.Interview, afterFirst!.Status);

        for (var i = 0; i < 4; i++) await Svc(s => s.AdvanceAsync(adoption.Id));
        var final = await Svc(s => s.GetAdoptionAsync(adoption.Id));
        Assert.Equal(AdoptionStatus.Finalized, final!.Status);
    }

    [Fact]
    public async Task Delete_ReturnsTrue_WhenFound()
    {
        var dog = await SeedDog();
        var adoption = await SeedAdoption(dog.Id);
        Assert.True(await Svc(s => s.DeleteAsync(adoption.Id)));
    }

    [Fact]
    public async Task Delete_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.DeleteAsync(99999)));
    }

    [Fact]
    public async Task GetDeleted_ReturnsEmpty_WhenNoneDeleted()
    {
        var dog = await SeedDog("LiveDog");
        await SeedAdoption(dog.Id);
        Assert.Empty(await Svc(s => s.GetDeletedAsync()));
    }

    [Fact]
    public async Task GetDeleted_ReturnsOnlyDeleted()
    {
        var dog = await SeedDog("MixedDog");
        await SeedAdoption(dog.Id);
        await SeedAdoption(dog.Id, applicant: "Gone", deletedAt: DateTime.UtcNow);
        var deleted = await Svc(s => s.GetDeletedAsync());
        Assert.Single(deleted);
        Assert.Equal("Gone", deleted[0].ApplicantName);
    }

    [Fact]
    public async Task Restore_ReturnsTrue_AndAppearsInActiveQuery()
    {
        var dog = await SeedDog("ReviveDog");
        var adoption = await SeedAdoption(dog.Id, applicant: "Revive", deletedAt: DateTime.UtcNow);
        Assert.True(await Svc(s => s.RestoreAsync(adoption.Id)));
        var restored = await Svc(s => s.GetAdoptionAsync(adoption.Id));
        Assert.NotNull(restored);
        Assert.Null(restored.DeletedAt);
    }

    [Fact]
    public async Task Restore_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.RestoreAsync(99999)));
    }

    [Fact]
    public async Task Purge_ReturnsTrue_OnlyForSoftDeleted()
    {
        var dog = await SeedDog("PurgeDog");
        var live = await SeedAdoption(dog.Id, applicant: "Live");
        var gone = await SeedAdoption(dog.Id, applicant: "Gone", deletedAt: DateTime.UtcNow);
        Assert.False(await Svc(s => s.PurgeAsync(live.Id)));
        Assert.True(await Svc(s => s.PurgeAsync(gone.Id)));
        Assert.Null(await ReadDirectAsync<Adoption>(gone.Id));
    }
}

public sealed class CapturingEmailSender : IShelterEmailSender
{
    public readonly List<(string To, string Subject, string Body)> Sent = [];

    public Task SendAsync(string to, string subject, string body)
    {
        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Verifies the full domain-event chain: Adoption.ChangeStatus raises
/// AdoptionStatusChanged → unit of work dispatches after save → handler emails.
/// </summary>
public class AdoptionStatusEmailTests : ServiceTestBase
{
    private readonly CapturingEmailSender _emailSender = new();

    protected override void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IShelterEmailSender>(_emailSender);

    private Task<T> Svc<T>(Func<IAdoptionService, Task<T>> action) => WithServiceAsync(action);

    private async Task<Adoption> SeedAdoptionWithEmail(string? email)
    {
        var dog = await SeedAsync(db => { var d = Dog.CheckIn("EmailDog", "Lab", 12, "M", 10m); db.Dogs.Add(d); return d; });
        return await SeedAsync(db =>
        {
            var adoption = Adoption.Submit(dog.Id, "Tester", email, null, AdoptionType.Adoption, null);
            db.Adoptions.Add(adoption);
            return adoption;
        });
    }

    [Fact]
    public async Task ChangeStatus_SendsEmail_WhenApplicantEmailSet()
    {
        var adoption = await SeedAdoptionWithEmail("applicant@test.com");

        await Svc(s => s.ChangeStatusAsync(new UpdateAdoptionStatusRequest(adoption.Id, AdoptionStatus.Interview, null)));

        Assert.Single(_emailSender.Sent);
        var (to, subject, body) = _emailSender.Sent[0];
        Assert.Equal("applicant@test.com", to);
        Assert.Contains("Tester", subject);
        Assert.Contains("Interview", body);
    }

    [Fact]
    public async Task ChangeStatus_DoesNotSendEmail_WhenNoApplicantEmail()
    {
        var adoption = await SeedAdoptionWithEmail(null);

        await Svc(s => s.ChangeStatusAsync(new UpdateAdoptionStatusRequest(adoption.Id, AdoptionStatus.Interview, null)));

        Assert.Empty(_emailSender.Sent);
    }

    [Fact]
    public async Task Update_DoesNotSendEmail_EvenWhenStatusChanges()
    {
        var adoption = await SeedAdoptionWithEmail("applicant@test.com");

        await Svc(s => s.UpdateAsync(new UpdateAdoptionRequest(
            adoption.Id, "Tester", "applicant@test.com", null, AdoptionType.Adoption, AdoptionStatus.Approved, null)));

        Assert.Empty(_emailSender.Sent);
    }

    [Fact]
    public async Task Advance_SendsEmail_ForNextStatus()
    {
        var adoption = await SeedAdoptionWithEmail("applicant@test.com");

        await Svc(s => s.AdvanceAsync(adoption.Id));

        Assert.Single(_emailSender.Sent);
        Assert.Contains("Interview", _emailSender.Sent[0].Body);
    }
}
