using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Refugio.Application.Contracts;
using Refugio.Domain.Entities;

namespace Refugio.Tests.Integration;

/// <summary>
/// Covers the browser-facing POST /api/{entity}/{id}/delete endpoints used by the
/// Blazor SSR forms on Funds (donations, expenses) and Calendar (events).
/// These are distinct from the DELETE-verb endpoints for external REST consumers.
/// </summary>
public class DeleteEndpointTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public DeleteEndpointTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AnonClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> ManagerClientAsync() =>
        await _factory.CreateAuthenticatedClientAsync();

    private async Task<HttpClient> VolunteerRoleClientAsync(string suffix)
    {
        var email = $"deldvol_{suffix}@test.com";
        await AnonClient().PostAsJsonAsync("/api/volunteers", new
        {
            Name = $"DelVol_{suffix}", Email = email, Phone = (string?)null,
            Role = "Volunteer", Notes = (string?)null, CanLogin = true, Password = "vol123456"
        });
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = email,
            ["password"] = "vol123456"
        });
        await client.PostAsync("/auth/login", form);
        return client;
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

    private async Task<int> CreateEventAsync(string title)
    {
        var start = DateTime.UtcNow.AddDays(1);
        var r = await AnonClient().PostAsJsonAsync("/api/events", new
        {
            Title = title, StartDateTime = start, EndDateTime = start.AddHours(2),
            Location = (string?)null, Description = (string?)null,
            EventType = "Adoption", AssignedVolunteers = (int?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateDogAsync(string name)
    {
        var r = await AnonClient().PostAsJsonAsync("/api/dogs", new
        {
            Name = name, Breed = "Mixed", AgeMonths = 12, Gender = "Male",
            WeightKg = 10m, PhotoUrl = (string?)null, Traits = (string?)null, Notes = (string?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateMedicalRecordAsync(int dogId)
    {
        var r = await AnonClient().PostAsJsonAsync($"/api/dogs/{dogId}/medical", new
        {
            DogId = dogId, VetName = "Dr.Test", Diagnosis = "TestDiag", Treatment = "TestTreat",
            Notes = (string?)null, NextVisitDate = (DateTime?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateMedicationAsync(int dogId)
    {
        var r = await AnonClient().PostAsJsonAsync($"/api/dogs/{dogId}/medications", new
        {
            DogId = dogId, Name = "TestMed", Dosage = "1mg", Frequency = "Daily",
            StartDate = DateTime.UtcNow, EndDate = (DateTime?)null
        });
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    // ── Anonymous → redirect to login ──────────────────────────

    [Fact]
    public async Task DeleteDonation_Anonymous_RedirectsToLogin()
    {
        var id = await CreateDonationAsync("AnonDelDonor");
        var response = await AnonClient().PostAsync($"/api/donations/{id}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task DeleteExpense_Anonymous_RedirectsToLogin()
    {
        var id = await CreateExpenseAsync("AnonDelExpense");
        var response = await AnonClient().PostAsync($"/api/expenses/{id}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task DeleteEvent_Anonymous_RedirectsToLogin()
    {
        var id = await CreateEventAsync("AnonDelEvent");
        var response = await AnonClient().PostAsync($"/api/events/{id}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    // ── Volunteer role (authenticated, not Manager) → AccessDenied ──

    [Fact]
    public async Task DeleteDonation_AsVolunteerRole_RedirectsToAccessDenied()
    {
        var id = await CreateDonationAsync("VolDelDonor");
        var vol = await VolunteerRoleClientAsync("donation");
        var response = await vol.PostAsync($"/api/donations/{id}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task DeleteExpense_AsVolunteerRole_RedirectsToAccessDenied()
    {
        var id = await CreateExpenseAsync("VolDelExpense");
        var vol = await VolunteerRoleClientAsync("expense");
        var response = await vol.PostAsync($"/api/expenses/{id}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task DeleteEvent_AsVolunteerRole_RedirectsToAccessDenied()
    {
        var id = await CreateEventAsync("VolDelEvent");
        var vol = await VolunteerRoleClientAsync("event");
        var response = await vol.PostAsync($"/api/events/{id}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
    }

    // ── Manager → redirect to listing + entity soft-deleted ────

    [Fact]
    public async Task DeleteDonation_AsManager_RedirectsToFunds_AndRecordGone()
    {
        var id = await CreateDonationAsync("MgrDelDonor");
        var manager = await ManagerClientAsync();

        var response = await manager.PostAsync($"/api/donations/{id}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/funds", response.Headers.Location?.OriginalString);

        var missing = await AnonClient().GetAsync($"/api/donations/{id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task DeleteExpense_AsManager_RedirectsToFunds_AndRecordGone()
    {
        var id = await CreateExpenseAsync("MgrDelExpense");
        var manager = await ManagerClientAsync();

        var response = await manager.PostAsync($"/api/expenses/{id}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/funds", response.Headers.Location?.OriginalString);

        var missing = await AnonClient().GetAsync($"/api/expenses/{id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsManager_RedirectsToCalendar_AndRecordGone()
    {
        var id = await CreateEventAsync("MgrDelEvent");
        var manager = await ManagerClientAsync();

        var response = await manager.PostAsync($"/api/events/{id}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/calendar", response.Headers.Location?.OriginalString);

        var missing = await AnonClient().GetAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // ── Medical records / medications (Health + DogDetail forms) ──

    [Fact]
    public async Task DeleteMedicalRecord_Anonymous_RedirectsToLogin()
    {
        var dogId = await CreateDogAsync("AnonMedDog");
        var medId = await CreateMedicalRecordAsync(dogId);
        var response = await AnonClient().PostAsync($"/api/medical/{medId}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task DeleteMedication_AsVolunteerRole_RedirectsToAccessDenied()
    {
        var dogId = await CreateDogAsync("VolMedDog");
        var medId = await CreateMedicationAsync(dogId);
        var vol = await VolunteerRoleClientAsync("medication");
        var response = await vol.PostAsync($"/api/medications/{medId}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task DeleteMedicalRecord_AsManager_DefaultsToDogPage_AndRecordGone()
    {
        var dogId = await CreateDogAsync("MgrMedDog");
        var medId = await CreateMedicalRecordAsync(dogId);
        var manager = await ManagerClientAsync();

        var response = await manager.PostAsync($"/api/medical/{medId}/delete?dogId={dogId}", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal($"/dogs/{dogId}", response.Headers.Location?.OriginalString);

        var records = await AnonClient().GetFromJsonAsync<List<MedicalRecordDto>>($"/api/dogs/{dogId}/medical");
        Assert.Empty(records!);
    }

    [Fact]
    public async Task DeleteMedicalRecord_AsManager_HonorsReturnUrl()
    {
        var dogId = await CreateDogAsync("ReturnUrlMedDog");
        var medId = await CreateMedicalRecordAsync(dogId);
        var manager = await ManagerClientAsync();

        var returnUrl = $"/health?dogId={dogId}";
        var response = await manager.PostAsync(
            $"/api/medical/{medId}/delete?returnUrl={Uri.EscapeDataString(returnUrl)}", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(returnUrl, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task DeleteMedication_AsManager_HonorsReturnUrl_AndRecordGone()
    {
        var dogId = await CreateDogAsync("ReturnUrlMedicationDog");
        var medId = await CreateMedicationAsync(dogId);
        var manager = await ManagerClientAsync();

        var returnUrl = $"/health?dogId={dogId}";
        var response = await manager.PostAsync(
            $"/api/medications/{medId}/delete?returnUrl={Uri.EscapeDataString(returnUrl)}", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(returnUrl, response.Headers.Location?.OriginalString);

        var meds = await AnonClient().GetFromJsonAsync<List<MedicationDto>>($"/api/dogs/{dogId}/medications");
        Assert.Empty(meds!);
    }
}
