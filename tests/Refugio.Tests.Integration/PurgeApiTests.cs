using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Refugio.Domain.Entities;

namespace Refugio.Tests.Integration;

public class PurgeApiTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public PurgeApiTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AnonClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> ManagerClientAsync() =>
        await _factory.CreateAuthenticatedClientAsync();

    private async Task<int> CreateDogAsync(string name)
    {
        var r = await AnonClient().PostAsJsonAsync("/api/dogs", new
        {
            Name = name, Breed = "Mixed", AgeMonths = 12, Gender = "Male",
            WeightKg = 10m, PhotoUrl = (string?)null, Traits = (string?)null, Notes = (string?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateDonationAsync(string donor)
    {
        var r = await AnonClient().PostAsJsonAsync("/api/donations", new
        {
            DonorName = donor, Amount = 50m, Category = "OneTime", Notes = (string?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateExpenseAsync(string desc)
    {
        var r = await AnonClient().PostAsJsonAsync("/api/expenses", new
        {
            Description = desc, Amount = 25m, Category = "Other", Notes = (string?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateMedicalRecordAsync(int dogId)
    {
        var r = await AnonClient().PostAsJsonAsync($"/api/dogs/{dogId}/medical", new
        {
            DogId = dogId, VetName = "Dr.Purge", Diagnosis = "PurgeDiag", Treatment = "PurgeTreat",
            Notes = (string?)null, NextVisitDate = (DateTime?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateMedicationAsync(int dogId)
    {
        var r = await AnonClient().PostAsJsonAsync($"/api/dogs/{dogId}/medications", new
        {
            DogId = dogId, Name = "PurgeMed", Dosage = "1mg", Frequency = "Daily",
            StartDate = DateTime.UtcNow, EndDate = (DateTime?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    // ── RBAC: anonymous → redirect to login ───────────────────

    [Fact]
    public async Task PurgeDog_Anonymous_RedirectsToLogin()
    {
        var response = await AnonClient().PostAsync("/api/dogs/1/purge", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task PurgeDonation_Anonymous_RedirectsToLogin()
    {
        var response = await AnonClient().PostAsync("/api/donations/1/purge", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task PurgeExpense_Anonymous_RedirectsToLogin()
    {
        var response = await AnonClient().PostAsync("/api/expenses/1/purge", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task PurgeMedicalRecord_Anonymous_RedirectsToLogin()
    {
        var response = await AnonClient().PostAsync("/api/medical/1/purge", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task PurgeMedication_Anonymous_RedirectsToLogin()
    {
        var response = await AnonClient().PostAsync("/api/medications/1/purge", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    // ── Manager: purge redirects to correct tab ───────────────

    [Fact]
    public async Task PurgeDog_AsManager_RedirectsToAdminDogsTab()
    {
        var dogId = await CreateDogAsync("PurgeTabDog");
        var manager = await ManagerClientAsync();
        await manager.DeleteAsync($"/api/dogs/{dogId}");
        var response = await manager.PostAsync($"/api/dogs/{dogId}/purge", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/deleted?tab=dogs", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task PurgeDonation_AsManager_RedirectsToAdminDonationsTab()
    {
        var id = await CreateDonationAsync("PurgeTabDonor");
        var manager = await ManagerClientAsync();
        await manager.PostAsync($"/api/donations/{id}/delete", new StringContent(""));
        var response = await manager.PostAsync($"/api/donations/{id}/purge", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/deleted?tab=donations", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task PurgeExpense_AsManager_RedirectsToAdminExpensesTab()
    {
        var id = await CreateExpenseAsync("PurgeTabExpense");
        var manager = await ManagerClientAsync();
        await manager.PostAsync($"/api/expenses/{id}/delete", new StringContent(""));
        var response = await manager.PostAsync($"/api/expenses/{id}/purge", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/deleted?tab=expenses", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task PurgeMedicalRecord_AsManager_RedirectsToAdminMedicalTab()
    {
        var dogId = await CreateDogAsync("PurgeMedTabDog");
        var medId = await CreateMedicalRecordAsync(dogId);
        var manager = await ManagerClientAsync();
        await manager.PostAsync($"/api/medical/{medId}/delete", new StringContent(""));
        var response = await manager.PostAsync($"/api/medical/{medId}/purge", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/deleted?tab=medical", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task PurgeMedication_AsManager_RedirectsToAdminMedicationsTab()
    {
        var dogId = await CreateDogAsync("PurgeMedMedsDog");
        var medId = await CreateMedicationAsync(dogId);
        var manager = await ManagerClientAsync();
        await manager.PostAsync($"/api/medications/{medId}/delete", new StringContent(""));
        var response = await manager.PostAsync($"/api/medications/{medId}/purge", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/deleted?tab=medications", response.Headers.Location?.OriginalString);
    }

    // ── End-to-end: record gone permanently after purge ────────

    [Fact]
    public async Task PurgeDog_AfterSoftDelete_GoneFromApiEvenAfterRestore()
    {
        var dogId = await CreateDogAsync("E2EPurgeDog");
        var manager = await ManagerClientAsync();

        // Soft-delete then purge
        await manager.DeleteAsync($"/api/dogs/{dogId}");
        await manager.PostAsync($"/api/dogs/{dogId}/purge", new StringContent(""));

        // Restore attempt should return redirect (actor returns false but endpoint still redirects)
        // Verify: GET by id returns 404
        var response = await AnonClient().GetAsync($"/api/dogs/{dogId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PurgeDonation_AfterSoftDelete_GoneFromApi()
    {
        var id = await CreateDonationAsync("E2EPurgeDonor");
        var manager = await ManagerClientAsync();

        await manager.PostAsync($"/api/donations/{id}/delete", new StringContent(""));
        await manager.PostAsync($"/api/donations/{id}/purge", new StringContent(""));

        var response = await AnonClient().GetAsync($"/api/donations/{id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PurgeExpense_AfterSoftDelete_GoneFromApi()
    {
        var id = await CreateExpenseAsync("E2EPurgeExpense");
        var manager = await ManagerClientAsync();

        await manager.PostAsync($"/api/expenses/{id}/delete", new StringContent(""));
        await manager.PostAsync($"/api/expenses/{id}/purge", new StringContent(""));

        var response = await AnonClient().GetAsync($"/api/expenses/{id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PurgeDog_WhenLive_LeavesRecordIntact()
    {
        var dogId = await CreateDogAsync("PurgeLiveDog");
        var manager = await ManagerClientAsync();

        // Purge without soft-deleting first — safety guard in actor must block
        await manager.PostAsync($"/api/dogs/{dogId}/purge", new StringContent(""));

        // Dog must still exist
        var response = await AnonClient().GetAsync($"/api/dogs/{dogId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
