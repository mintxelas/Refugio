using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

/// <summary>
/// Proves the REST DELETE-verb endpoints require the Manager role, same as the parallel
/// POST .../delete endpoints. Before the fix, DELETE bypassed the Manager gate entirely.
/// </summary>
public class DeleteVerbAuthorizationTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public DeleteVerbAuthorizationTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AnonClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> ManagerClientAsync() =>
        await _factory.CreateAuthenticatedClientAsync();

    private async Task<HttpClient> VolunteerRoleClientAsync(string suffix)
    {
        var email = $"delverb_{suffix}@test.com";
        await (await ManagerClientAsync()).PostAsJsonAsync("/api/volunteers", new
        {
            Name = $"DelVerbVol_{suffix}", Email = email, Phone = (string?)null,
            Role = "Volunteer", Notes = (string?)null, CanLogin = true, Password = "vol123456"
        });
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await ShelterWebFactory.AttachCsrfTokenAsync(client);
        await client.PostAsJsonAsync("/api/auth/login", new { email, password = "vol123456" });
        await ShelterWebFactory.AttachCsrfTokenAsync(client);
        return client;
    }

    private static async Task<int> IdFromAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

    private async Task<int> CreateDonationAsync() =>
        await IdFromAsync(await (await ManagerClientAsync()).PostAsJsonAsync("/api/donations", new
        {
            DonorName = "DelVerbDonor", Amount = 10m, Category = "OneTime", Notes = (string?)null
        }));

    private async Task<int> CreateTaskAsync() =>
        await IdFromAsync(await (await ManagerClientAsync()).PostAsJsonAsync("/api/tasks", new
        {
            Title = "DelVerbTask", DueDateTime = DateTime.UtcNow.AddDays(1),
            Notes = (string?)null, Location = (string?)null, AssignedVolunteerId = (int?)null
        }));

    [Fact]
    public async Task DeleteDonation_ViaDeleteVerb_AsVolunteerRole_IsForbidden()
    {
        var id = await CreateDonationAsync();
        var vol = await VolunteerRoleClientAsync("donation");

        var response = await vol.DeleteAsync($"/api/donations/{id}");
        Assert.NotEqual(HttpStatusCode.NoContent, response.StatusCode);

        var stillThere = await (await ManagerClientAsync()).GetAsync($"/api/donations/{id}");
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_ViaDeleteVerb_AsVolunteerRole_IsForbidden()
    {
        var id = await CreateTaskAsync();
        var vol = await VolunteerRoleClientAsync("task");

        var response = await vol.DeleteAsync($"/api/tasks/{id}");
        Assert.NotEqual(HttpStatusCode.NoContent, response.StatusCode);

        var stillThere = await (await ManagerClientAsync()).GetFromJsonAsync<List<JsonElement>>("/api/tasks?includeCompleted=true");
        Assert.Contains(stillThere!, t => t.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task DeleteDonation_ViaDeleteVerb_Anonymous_IsRejected()
    {
        var id = await CreateDonationAsync();
        var response = await AnonClient().DeleteAsync($"/api/donations/{id}");
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Found,
            $"Expected 401 or redirect, got {response.StatusCode}");
    }

    [Fact]
    public async Task DeleteDonation_ViaDeleteVerb_AsManager_Succeeds()
    {
        var id = await CreateDonationAsync();
        var manager = await ManagerClientAsync();

        var response = await manager.DeleteAsync($"/api/donations/{id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var missing = await manager.GetAsync($"/api/donations/{id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
