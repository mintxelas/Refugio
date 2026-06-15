using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

public class RbacTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public RbacTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AnonClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<int> CreateDogAsync()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/api/dogs", new
        {
            Name = "RbacTestDog",
            Breed = "Test",
            AgeMonths = 12,
            Gender = "Male",
            WeightKg = 10.0m,
            PhotoUrl = (string?)null,
            Traits = (string?)null,
            Notes = (string?)null,
            ArrivalDate = DateTime.UtcNow
        });
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task UnauthenticatedPost_ToManagerEndpoint_RedirectsToLogin()
    {
        var dogId = await CreateDogAsync();
        var response = await AnonClient().PostAsync($"/api/dogs/{dogId}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task ManagerPost_ToManagerEndpoint_RedirectsToDogs()
    {
        var dogId = await CreateDogAsync();
        var managerClient = await _factory.CreateAuthenticatedClientAsync();
        var response = await managerClient.PostAsync($"/api/dogs/{dogId}/delete", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/dogs", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task UnauthenticatedGet_ToExportEndpoint_RedirectsToLogin()
    {
        var response = await AnonClient().GetAsync("/api/export/donations");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task AuthenticatedGet_ToExportEndpoint_ReturnsOk()
    {
        var managerClient = await _factory.CreateAuthenticatedClientAsync();
        var response = await managerClient.GetAsync("/api/export/donations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
    }
}
