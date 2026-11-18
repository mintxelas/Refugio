using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

/// <summary>
/// Regression tests for the November 2026 hardening pass. Each test reproduces one audit finding.
/// </summary>
public class HardeningTests : IClassFixture<ShelterWebFactory>
{
    private const string VolunteerPassword = "vol123456";
    private readonly ShelterWebFactory _factory;

    public HardeningTests(ShelterWebFactory factory) => _factory = factory;

    private HttpClient AnonClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private Task<HttpClient> ManagerClientAsync() => _factory.CreateAuthenticatedClientAsync();

    private static async Task<int> IdFromAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

    private async Task<int> CreateVolunteerAsync(string email) =>
        await IdFromAsync(await (await ManagerClientAsync()).PostAsJsonAsync("/api/volunteers", new
        {
            Name = email, Email = email, Phone = (string?)null, Role = "Volunteer",
            Notes = (string?)null, CanLogin = true, Password = VolunteerPassword
        }));

    private async Task<HttpClient> LoginAsync(string email, string password = VolunteerPassword)
    {
        var client = AnonClient();
        await ShelterWebFactory.AttachCsrfTokenAsync(client);
        await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        await ShelterWebFactory.AttachCsrfTokenAsync(client);
        return client;
    }

    private async Task<HttpClient> VolunteerClientAsync(string email)
    {
        await CreateVolunteerAsync(email);
        return await LoginAsync(email);
    }

    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        var response = await AnonClient().GetAsync("/api/dashboard");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("same-origin", response.Headers.GetValues("Referrer-Policy").Single());
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("script-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("object-src 'none'", csp);
    }

    [Fact]
    public async Task Cors_OutsideDevelopment_GrantsNoCrossOriginCredentials()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard");
        request.Headers.Add("Origin", "http://localhost:8081");

        var response = await AnonClient().SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task DeleteEvent_ViaDeleteVerb_AsVolunteerRole_IsForbidden()
    {
        var created = await (await ManagerClientAsync()).PostAsJsonAsync("/api/events", new
        {
            Title = "HardenEvent", StartDateTime = DateTime.UtcNow.AddDays(1),
            EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(1), Location = (string?)null,
            Description = (string?)null, EventType = "Walk", AssignedVolunteers = (int?)null
        });
        var id = await IdFromAsync(created);
        var vol = await VolunteerClientAsync("harden_event@test.com");

        var response = await vol.DeleteAsync($"/api/events/{id}");

        Assert.NotEqual(HttpStatusCode.NoContent, response.StatusCode);
        var stillThere = await (await ManagerClientAsync()).GetAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
    }

    [Fact]
    public async Task GetDog_Anonymous_DoesNotExposeMedicalHistory()
    {
        var manager = await ManagerClientAsync();
        var dogId = await IdFromAsync(await manager.PostAsJsonAsync("/api/dogs", new
        {
            Name = "HardenDog", Breed = "Mix", AgeMonths = 12, Gender = "Male", WeightKg = 10m,
            PhotoUrl = (string?)null, Traits = (string?)null, Notes = (string?)null, ArrivalDate = DateTime.UtcNow
        }));
        await manager.PostAsJsonAsync($"/api/dogs/{dogId}/medical", new
        {
            DogId = dogId, VetName = "Dr Secret", Diagnosis = "SecretDiagnosis", Treatment = "Rest",
            Notes = (string?)null, NextVisitDate = (DateTime?)null
        });

        var anonBody = await AnonClient().GetStringAsync($"/api/dogs/{dogId}");
        var managerBody = await manager.GetStringAsync($"/api/dogs/{dogId}");

        Assert.Contains("HardenDog", anonBody);
        Assert.DoesNotContain("SecretDiagnosis", anonBody);
        Assert.Contains("SecretDiagnosis", managerBody);
    }

    [Fact]
    public async Task VolunteerPhoto_OtherVolunteer_CannotDeleteIt()
    {
        var victimId = await CreateVolunteerAsync("harden_photo_victim@test.com");
        var manager = await ManagerClientAsync();
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]), "Photo", "p.png");
        await manager.PostAsync($"/api/volunteers/{victimId}/photo", form);
        var attacker = await VolunteerClientAsync("harden_photo_attacker@test.com");

        await attacker.PostAsync($"/api/volunteers/{victimId}/photo/delete", null);

        var victim = await manager.GetFromJsonAsync<JsonElement>($"/api/volunteers/{victimId}");
        Assert.Equal(JsonValueKind.String, victim.GetProperty("photoUrl").ValueKind);
    }

    [Fact]
    public async Task VolunteerPhoto_Self_CanDeleteIt()
    {
        var selfId = await CreateVolunteerAsync("harden_photo_self@test.com");
        var self = await LoginAsync("harden_photo_self@test.com");

        var response = await self.PostAsync($"/api/volunteers/{selfId}/photo/delete", null);

        Assert.Equal($"/volunteers/{selfId}", response.Headers.Location?.OriginalString);
    }

    private async Task UpdateVolunteerAsync(int id, string email, string role, string? newPassword = null) =>
        await (await ManagerClientAsync()).PutAsJsonAsync($"/api/volunteers/{id}", new
        {
            Id = id, Name = email, Email = email, Phone = (string?)null, Role = role, Notes = (string?)null,
            CanLogin = true, Status = "Active", NewPassword = newPassword, PreferredLanguage = (string?)null
        });

    [Fact]
    public async Task Login_InactiveVolunteer_IsRejected()
    {
        var id = await CreateVolunteerAsync("harden_inactive@test.com");
        await (await ManagerClientAsync()).PostAsync($"/api/volunteers/{id}/deactivate", null);

        var client = AnonClient();
        await ShelterWebFactory.AttachCsrfTokenAsync(client);
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "harden_inactive@test.com", password = VolunteerPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_IsRevoked_WhenVolunteerDeactivated()
    {
        var id = await CreateVolunteerAsync("harden_deact_session@test.com");
        var vol = await LoginAsync("harden_deact_session@test.com");
        Assert.Equal(HttpStatusCode.OK, (await vol.GetAsync("/api/auth/me")).StatusCode);

        await (await ManagerClientAsync()).PostAsync($"/api/volunteers/{id}/deactivate", null);

        Assert.NotEqual(HttpStatusCode.OK, (await vol.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Session_IsRevoked_WhenPasswordResetByManager()
    {
        const string email = "harden_pwreset_session@test.com";
        var id = await CreateVolunteerAsync(email);
        var vol = await LoginAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await vol.GetAsync("/api/auth/me")).StatusCode);

        await UpdateVolunteerAsync(id, email, "Volunteer", newPassword: "brand-new-password");

        Assert.NotEqual(HttpStatusCode.OK, (await vol.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Session_SurvivesOwnPasswordChange()
    {
        const string email = "harden_selfpw@test.com";
        await CreateVolunteerAsync(email);
        var vol = await LoginAsync(email);

        var change = await vol.PostAsJsonAsync("/api/auth/change-password", new
        {
            CurrentPassword = VolunteerPassword, NewPassword = "another-password", ConfirmPassword = "another-password"
        });

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await vol.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Session_LosesManagerRights_WhenDemoted()
    {
        const string email = "harden_demoted@test.com";
        var id = await CreateVolunteerAsync(email);
        await UpdateVolunteerAsync(id, email, "Manager");
        var exManager = await LoginAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await exManager.GetAsync("/api/volunteers/deleted")).StatusCode);

        await UpdateVolunteerAsync(id, email, "Volunteer");

        Assert.NotEqual(HttpStatusCode.OK, (await exManager.GetAsync("/api/volunteers/deleted")).StatusCode);
    }
}
