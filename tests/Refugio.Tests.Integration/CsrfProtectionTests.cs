using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

/// <summary>
/// Proves the CSRF (antiforgery) protection added to every mutating /api/* endpoint actually
/// rejects requests missing a valid X-XSRF-TOKEN header, while leaving safe (GET) requests and
/// correctly-tokened requests unaffected.
/// </summary>
public class CsrfProtectionTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public CsrfProtectionTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AntiforgeryTokenEndpoint_ReturnsNonEmptyToken_Anonymously()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/antiforgery/token");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task PostDog_WithoutCsrfHeader_IsRejected()
    {
        // Fully authenticated (login itself needs a valid token too), then strip the header
        // before the call under test to isolate /api/dogs's own antiforgery requirement.
        var client = await _factory.CreateAuthenticatedClientAsync();
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var response = await client.PostAsJsonAsync("/api/dogs", new
        {
            Name = "NoCsrfDog", Breed = "Test", AgeMonths = 12, Gender = "Male",
            WeightKg = 10m, ArrivalDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostDog_WithCsrfHeader_Succeeds()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/dogs", new
        {
            Name = "WithCsrfDog", Breed = "Test", AgeMonths = 12, Gender = "Male",
            WeightKg = 10m, ArrivalDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetDogs_WithoutCsrfHeader_Succeeds()
    {
        // Safe methods are exempt from antiforgery validation entirely.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/dogs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfToken_FetchedBeforeLogin_NoLongerValidatesAfterLogin()
    {
        // The antiforgery token embeds the caller's identity at issue time. A token fetched
        // anonymously must be refreshed after login, or requests start failing — this is exactly
        // the bug this test guards against regressing.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await ShelterWebFactory.AttachCsrfTokenAsync(client); // anonymous token
        await client.PostAsJsonAsync("/api/auth/login",
            new { email = "elena@havensanctuary.org", password = "shelter123" });
        // Deliberately NOT refreshing the token here.

        var response = await client.PostAsJsonAsync("/api/dogs", new
        {
            Name = "StaleCsrfDog", Breed = "Test", AgeMonths = 12, Gender = "Male",
            WeightKg = 10m, ArrivalDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
