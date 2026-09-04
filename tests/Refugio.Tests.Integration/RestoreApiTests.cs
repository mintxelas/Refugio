using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Refugio.Application.Contracts;
using Refugio.Domain.Entities;

namespace Refugio.Tests.Integration;

public class RestoreApiTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public RestoreApiTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AnonClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> ManagerClientAsync() =>
        await _factory.CreateAuthenticatedClientAsync();

    private async Task<int> CreateDogAsync(string name = "RestoreTestDog")
    {
        var r = await (await ManagerClientAsync()).PostAsJsonAsync("/api/dogs", new
        {
            Name = name, Breed = "Mixed", AgeMonths = 12, Gender = "Male",
            WeightKg = 10m, PhotoUrl = (string?)null, Traits = (string?)null, Notes = (string?)null,
            ArrivalDate = DateTime.UtcNow
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateDonationAsync(string donor = "RestoreDonor")
    {
        var r = await (await ManagerClientAsync()).PostAsJsonAsync("/api/donations", new
        {
            DonorName = donor, Amount = 50m, Category = "OneTime", Notes = (string?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateExpenseAsync(string desc = "RestoreExpense")
    {
        var r = await (await ManagerClientAsync()).PostAsJsonAsync("/api/expenses", new
        {
            Description = desc, Amount = 25m, Category = "Other", Notes = (string?)null,
            taxLines = new[] { new { ivaPercent = 0m, @base = 25m, importe = 0m } }
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateMedicalRecordAsync(int dogId)
    {
        var r = await (await ManagerClientAsync()).PostAsJsonAsync($"/api/dogs/{dogId}/medical", new
        {
            DogId = dogId, VetName = "Dr.Test", Diagnosis = "TestDiag", Treatment = "TestTreat",
            Notes = (string?)null, NextVisitDate = (DateTime?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateMedicationAsync(int dogId)
    {
        var r = await (await ManagerClientAsync()).PostAsJsonAsync($"/api/dogs/{dogId}/medications", new
        {
            DogId = dogId, Name = "TestMed", Dosage = "1mg", Frequency = "Daily",
            StartDate = DateTime.UtcNow, EndDate = (DateTime?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    // ── RBAC: unauthenticated → redirect to login ──────────────

    [Fact]
    public async Task RestoreDog_Anonymous_RedirectsToLogin()
    {
        var dogId = await CreateDogAsync("RbacRestoreDog");
        var response = await AnonClient().PostAsync($"/api/dogs/{dogId}/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task RestoreDonation_Anonymous_RedirectsToLogin()
    {
        var id = await CreateDonationAsync("RbacRestoreDonor");
        var response = await AnonClient().PostAsync($"/api/donations/{id}/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task RestoreExpense_Anonymous_RedirectsToLogin()
    {
        var id = await CreateExpenseAsync("RbacRestoreExpense");
        var response = await AnonClient().PostAsync($"/api/expenses/{id}/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task RestoreMedicalRecord_Anonymous_RedirectsToLogin()
    {
        var response = await AnonClient().PostAsync("/api/medical/1/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task RestoreMedication_Anonymous_RedirectsToLogin()
    {
        var response = await AnonClient().PostAsync("/api/medications/1/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    // ── Manager → correct redirect tab ────────────────────────

    [Fact]
    public async Task RestoreDog_AsManager_RedirectsToAdminDogTab()
    {
        var dogId = await CreateDogAsync("ManagerRestoreDog");
        var manager = await ManagerClientAsync();
        await manager.DeleteAsync($"/api/dogs/{dogId}");
        var response = await manager.PostAsync($"/api/dogs/{dogId}/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/deleted?tab=dogs", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RestoreDonation_AsManager_RedirectsToAdminDonationsTab()
    {
        var id = await CreateDonationAsync("TabDonor");
        var manager = await ManagerClientAsync();
        await manager.PostAsync($"/api/donations/{id}/delete", new StringContent(""));
        var response = await manager.PostAsync($"/api/donations/{id}/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/deleted?tab=donations", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RestoreExpense_AsManager_RedirectsToAdminExpensesTab()
    {
        var id = await CreateExpenseAsync("TabExpense");
        var manager = await ManagerClientAsync();
        await manager.PostAsync($"/api/expenses/{id}/delete", new StringContent(""));
        var response = await manager.PostAsync($"/api/expenses/{id}/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/deleted?tab=expenses", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RestoreMedicalRecord_AsManager_RedirectsToAdminMedicalTab()
    {
        var dogId = await CreateDogAsync("MedTabDog");
        var medId = await CreateMedicalRecordAsync(dogId);
        var manager = await ManagerClientAsync();
        await manager.PostAsync($"/api/medical/{medId}/delete", new StringContent(""));
        var response = await manager.PostAsync($"/api/medical/{medId}/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/deleted?tab=medical", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RestoreMedication_AsManager_RedirectsToAdminMedicationsTab()
    {
        var dogId = await CreateDogAsync("MedTabMedsDog");
        var medId = await CreateMedicationAsync(dogId);
        var manager = await ManagerClientAsync();
        await manager.PostAsync($"/api/medications/{medId}/delete", new StringContent(""));
        var response = await manager.PostAsync($"/api/medications/{medId}/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/deleted?tab=medications", response.Headers.Location?.OriginalString);
    }

    // ── End-to-end: record reappears after restore ─────────────

    [Fact]
    public async Task RestoreDog_AfterSoftDelete_ReappearsInGetById()
    {
        var dogId = await CreateDogAsync("E2ERestoreDog");
        var manager = await ManagerClientAsync();
        await manager.DeleteAsync($"/api/dogs/{dogId}");

        var missingResponse = await AnonClient().GetAsync($"/api/dogs/{dogId}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);

        await manager.PostAsync($"/api/dogs/{dogId}/restore", new StringContent(""));

        var restoredResponse = await AnonClient().GetAsync($"/api/dogs/{dogId}");
        Assert.Equal(HttpStatusCode.OK, restoredResponse.StatusCode);
    }

    [Fact]
    public async Task RestoreDonation_AfterSoftDelete_ReappearsInGetById()
    {
        var id = await CreateDonationAsync("E2EDonor");
        var manager = await ManagerClientAsync();
        await manager.PostAsync($"/api/donations/{id}/delete", new StringContent(""));

        var missing = await manager.GetAsync($"/api/donations/{id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        await manager.PostAsync($"/api/donations/{id}/restore", new StringContent(""));

        var restored = await manager.GetAsync($"/api/donations/{id}");
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
    }

    [Fact]
    public async Task RestoreExpense_AfterSoftDelete_ReappearsInGetById()
    {
        var id = await CreateExpenseAsync("E2EExpense");
        var manager = await ManagerClientAsync();
        await manager.PostAsync($"/api/expenses/{id}/delete", new StringContent(""));

        var missing = await manager.GetAsync($"/api/expenses/{id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        await manager.PostAsync($"/api/expenses/{id}/restore", new StringContent(""));

        var restored = await manager.GetAsync($"/api/expenses/{id}");
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
    }

    // ── Parent-dog constraint ──────────────────────────────────

    [Fact]
    public async Task RestoreMedicalRecord_WhenParentDogDeleted_RecordRemainsDeleted()
    {
        var manager = await ManagerClientAsync();

        var dogId = await CreateDogAsync("ParentDogMedRecord");
        var medId = await CreateMedicalRecordAsync(dogId);

        // Soft-delete the record, then the dog
        await manager.PostAsync($"/api/medical/{medId}/delete", new StringContent(""));
        await manager.DeleteAsync($"/api/dogs/{dogId}");

        // Attempt restore — actor blocks because dog is deleted
        await manager.PostAsync($"/api/medical/{medId}/restore", new StringContent(""));

        // Restore the dog so we can query its records
        await manager.PostAsync($"/api/dogs/{dogId}/restore", new StringContent(""));

        // Medical record must still be soft-deleted
        var records = await manager.GetFromJsonAsync<List<MedicalRecordDto>>($"/api/dogs/{dogId}/medical");
        Assert.NotNull(records);
        Assert.Empty(records);
    }

    [Fact]
    public async Task RestoreMedication_WhenParentDogDeleted_MedicationRemainsDeleted()
    {
        var manager = await ManagerClientAsync();

        var dogId = await CreateDogAsync("ParentDogMedication");
        var medId = await CreateMedicationAsync(dogId);

        // Soft-delete the medication, then the dog
        await manager.PostAsync($"/api/medications/{medId}/delete", new StringContent(""));
        await manager.DeleteAsync($"/api/dogs/{dogId}");

        // Attempt restore — actor blocks because dog is deleted
        await manager.PostAsync($"/api/medications/{medId}/restore", new StringContent(""));

        // Restore the dog so we can query its medications
        await manager.PostAsync($"/api/dogs/{dogId}/restore", new StringContent(""));

        // Medication must still be soft-deleted
        var meds = await manager.GetFromJsonAsync<List<MedicationDto>>($"/api/dogs/{dogId}/medications");
        Assert.NotNull(meds);
        Assert.Empty(meds);
    }

    [Fact]
    public async Task RestoreMedicalRecord_WhenDogAlive_RecordReappears()
    {
        var manager = await ManagerClientAsync();

        var dogId = await CreateDogAsync("AliveDogMedRecord");
        var medId = await CreateMedicalRecordAsync(dogId);

        await manager.PostAsync($"/api/medical/{medId}/delete", new StringContent(""));

        var before = await manager.GetFromJsonAsync<List<MedicalRecordDto>>($"/api/dogs/{dogId}/medical");
        Assert.Empty(before!);

        await manager.PostAsync($"/api/medical/{medId}/restore", new StringContent(""));

        var after = await manager.GetFromJsonAsync<List<MedicalRecordDto>>($"/api/dogs/{dogId}/medical");
        Assert.Single(after!);
    }

    // ── Volunteer role (authenticated, not Manager) → AccessDenied ──

    private async Task<HttpClient> VolunteerRoleClientAsync(string suffix)
    {
        var email = $"volrole_{suffix}@test.com";
        await (await ManagerClientAsync()).PostAsJsonAsync("/api/volunteers", new
        {
            Name = $"TestVol_{suffix}", Email = email, Phone = (string?)null,
            Role = "Volunteer", Notes = (string?)null, CanLogin = true, Password = "vol123456"
        });
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await ShelterWebFactory.AttachCsrfTokenAsync(client);
        await client.PostAsJsonAsync("/api/auth/login", new { email, password = "vol123456" });
        await ShelterWebFactory.AttachCsrfTokenAsync(client);
        return client;
    }

    [Fact]
    public async Task RestoreDog_AsVolunteerRole_RedirectsToAccessDenied()
    {
        var dogId = await CreateDogAsync("VrbacDog");
        var vol = await VolunteerRoleClientAsync("dog");
        var response = await vol.PostAsync($"/api/dogs/{dogId}/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task RestoreDonation_AsVolunteerRole_RedirectsToAccessDenied()
    {
        var id = await CreateDonationAsync("VrbacDonor");
        var vol = await VolunteerRoleClientAsync("donation");
        var response = await vol.PostAsync($"/api/donations/{id}/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task RestoreExpense_AsVolunteerRole_RedirectsToAccessDenied()
    {
        var id = await CreateExpenseAsync("VrbacExpense");
        var vol = await VolunteerRoleClientAsync("expense");
        var response = await vol.PostAsync($"/api/expenses/{id}/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task RestoreMedicalRecord_AsVolunteerRole_RedirectsToAccessDenied()
    {
        var vol = await VolunteerRoleClientAsync("medical");
        var response = await vol.PostAsync("/api/medical/1/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task RestoreMedication_AsVolunteerRole_RedirectsToAccessDenied()
    {
        var vol = await VolunteerRoleClientAsync("medication");
        var response = await vol.PostAsync("/api/medications/1/restore", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
    }
}
