using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Refugio.Domain.Entities;

namespace Refugio.Tests.Integration;

public class VolunteerApiTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;
    private readonly HttpClient _client;

    public VolunteerApiTests(ShelterWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private async Task<int> CreateVolunteerAsync(string name = "Test Volunteer", string email = "volunteer@test.com")
    {
        var response = await _client.PostAsJsonAsync("/api/volunteers", new
        {
            Name = name,
            Email = email,
            Phone = (string?)null,
            Role = "Volunteer",
            Notes = (string?)null,
            CanLogin = false,
            Password = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task GetVolunteers_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/volunteers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVolunteers_ReturnsJsonArray()
    {
        var volunteers = await _client.GetFromJsonAsync<List<Volunteer>>("/api/volunteers");
        Assert.NotNull(volunteers);
    }

    [Fact]
    public async Task GetVolunteer_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/volunteers/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostVolunteer_CreatesAndReturns201()
    {
        var response = await _client.PostAsJsonAsync("/api/volunteers", new
        {
            Name = "New Volunteer",
            Email = "new.volunteer@test.com",
            Phone = "555-0001",
            Role = "Volunteer",
            Notes = "Created by integration test",
            CanLogin = false,
            Password = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("id").GetInt32() > 0);
        Assert.Equal("New Volunteer", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetVolunteer_AfterCreate_ReturnsIt()
    {
        var id = await CreateVolunteerAsync("GetAfterCreate", "getaftercreate@test.com");
        var response = await _client.GetAsync($"/api/volunteers/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal("GetAfterCreate", JsonDocument.Parse(json).RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task PutVolunteer_UpdatesName()
    {
        var id = await CreateVolunteerAsync("OriginalName", "updatename@test.com");

        var response = await _client.PutAsJsonAsync($"/api/volunteers/{id}", new
        {
            Id = id,
            Name = "UpdatedName",
            Email = "updatename@test.com",
            Phone = (string?)null,
            Role = "Volunteer",
            Notes = (string?)null,
            CanLogin = false,
            Status = 0, // Active
            NewPassword = (string?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal("UpdatedName", JsonDocument.Parse(json).RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task PutVolunteer_UpdatesRole()
    {
        var id = await CreateVolunteerAsync("RoleUpdate", "roleupdate@test.com");

        var response = await _client.PutAsJsonAsync($"/api/volunteers/{id}", new
        {
            Id = id,
            Name = "RoleUpdate",
            Email = "roleupdate@test.com",
            Phone = (string?)null,
            Role = "Manager",
            Notes = (string?)null,
            CanLogin = false,
            Status = 0,
            NewPassword = (string?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal("Manager", JsonDocument.Parse(json).RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task PutVolunteer_NonExistent_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/volunteers/99999", new
        {
            Id = 99999,
            Name = "Ghost",
            Email = "ghost@test.com",
            Phone = (string?)null,
            Role = "Volunteer",
            Notes = (string?)null,
            CanLogin = false,
            Status = 0,
            NewPassword = (string?)null
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutVolunteerStatus_UpdatesToInactive()
    {
        var id = await CreateVolunteerAsync("StatusChange", "statuschange@test.com");

        var response = await _client.PutAsJsonAsync($"/api/volunteers/{id}/status", new
        {
            Id = id,
            Status = 1 // Inactive
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal(1, JsonDocument.Parse(json).RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetVolunteers_FilterByStatus_ReturnsMatchingOnly()
    {
        await CreateVolunteerAsync("ActiveVol", "activevol@test.com");

        var volunteers = await _client.GetFromJsonAsync<List<JsonElement>>("/api/volunteers?status=0"); // Active
        Assert.NotNull(volunteers);
        Assert.All(volunteers, v => Assert.Equal(0, v.GetProperty("status").GetInt32()));
    }

    [Fact]
    public async Task DeleteVolunteer_AfterCreate_Returns204()
    {
        var id = await CreateVolunteerAsync("ToDelete", "todelete@test.com");
        var response = await _client.DeleteAsync($"/api/volunteers/{id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVolunteer_NonExistent_Returns404()
    {
        var response = await _client.DeleteAsync("/api/volunteers/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ActivateVolunteer_Unauthenticated_RedirectsToLogin()
    {
        var id = await CreateVolunteerAsync("ActivateTest", "activatetest@test.com");
        var response = await _client.PostAsync($"/api/volunteers/{id}/activate", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task ActivateVolunteer_AsManager_RedirectsToVolunteers()
    {
        var id = await CreateVolunteerAsync("ActivateAsManager", "activateasmanager@test.com");
        var authClient = await _factory.CreateAuthenticatedClientAsync();
        var response = await authClient.PostAsync($"/api/volunteers/{id}/activate", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/volunteers", response.Headers.Location?.OriginalString);
    }
}
