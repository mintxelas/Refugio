using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

public class ReportsApiTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public ReportsApiTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AnonClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> ManagerClientAsync() =>
        await _factory.CreateAuthenticatedClientAsync();

    [Fact]
    public async Task GetAdoptionConversion_AsAuthenticated_ReturnsOk()
    {
        var client = await ManagerClientAsync();
        var response = await client.GetAsync("/api/reports/adoption-conversion");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAdoptionConversion_AsAnon_RedirectsToLogin()
    {
        var response = await AnonClient().GetAsync("/api/reports/adoption-conversion");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task GetShelterStay_AsAuthenticated_ReturnsOk()
    {
        var client = await ManagerClientAsync();
        var response = await client.GetAsync("/api/reports/shelter-stay");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetShelterStay_AsAnon_RedirectsToLogin()
    {
        var response = await AnonClient().GetAsync("/api/reports/shelter-stay");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task GetReportsPage_AsAuthenticated_ReturnsOk()
    {
        var client = await ManagerClientAsync();
        var response = await client.GetAsync("/reports");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDogCheckinPage_AsAuthenticated_ReturnsOk()
    {
        var client = await ManagerClientAsync();
        var response = await client.GetAsync("/dogs/new");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDogCheckinPage_AsAnon_RedirectsToLogin()
    {
        var response = await AnonClient().GetAsync("/dogs/new");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }
}
