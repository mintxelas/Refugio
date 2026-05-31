using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Domain.Entities;
using Refugio.Domain.Helpers;
using Refugio.Infrastructure.Data;

namespace Refugio.Tests.Integration;

public class AuthEndpointTests : IClassFixture<ShelterWebFactory>
{
    private const string CultureCookieName = ".AspNetCore.Culture";
    private readonly ShelterWebFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointTests(ShelterWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private async Task<int> SeedLoginVolunteerAsync(string email, string password, string? preferredLanguage)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        var v = new Volunteer
        {
            Name = email, Email = email, Role = "Volunteer",
            CanLogin = true, PasswordHash = PasswordHelper.Hash(password),
            PreferredLanguage = preferredLanguage
        };
        db.Volunteers.Add(v);
        await db.SaveChangesAsync();
        return v.Id;
    }

    private static string CookieHeaders(HttpResponseMessage r)
        => r.Headers.TryGetValues("Set-Cookie", out var v) ? string.Join("\n", v) : "";

    [Fact]
    public async Task UnauthenticatedRequest_ToProtectedPage_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToHome()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "elena@havensanctuary.org",
            ["password"] = "shelter123"
        });
        var response = await _client.PostAsync("/auth/login", form);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_WithWrongPassword_RedirectsToLoginWithError()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "elena@havensanctuary.org",
            ["password"] = "wrongpassword"
        });
        var response = await _client.PostAsync("/auth/login", form);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("error=1", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task Login_WithUnknownEmail_RedirectsToLoginWithError()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "nobody@nowhere.com",
            ["password"] = "shelter123"
        });
        var response = await _client.PostAsync("/auth/login", form);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("error=1", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task Login_WithPreferredLanguage_SetsCultureCookie()
    {
        await SeedLoginVolunteerAsync("languser@test.com", "pass1234", "es-ES");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "languser@test.com",
            ["password"] = "pass1234"
        });
        var response = await _client.PostAsync("/auth/login", form);
        var cookies = CookieHeaders(response);
        Assert.Contains(CultureCookieName, cookies);
        Assert.Contains("es-ES", cookies);
    }

    [Fact]
    public async Task Login_WithUnsupportedPreferredLanguage_DoesNotSetCultureCookie()
    {
        await SeedLoginVolunteerAsync("badlang@test.com", "pass1234", "xx-XX");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "badlang@test.com",
            ["password"] = "pass1234"
        });
        var response = await _client.PostAsync("/auth/login", form);
        Assert.DoesNotContain(CultureCookieName, CookieHeaders(response));
    }

    [Fact]
    public async Task Login_WithoutPreferredLanguage_DoesNotSetCultureCookie()
    {
        await SeedLoginVolunteerAsync("nolang@test.com", "pass1234", null);
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "nolang@test.com",
            ["password"] = "pass1234"
        });
        var response = await _client.PostAsync("/auth/login", form);
        Assert.DoesNotContain(CultureCookieName, CookieHeaders(response));
    }

    [Fact]
    public async Task VolunteerEdit_PreselectsActiveCulture_WhenNoSavedLanguage()
    {
        var id = await SeedLoginVolunteerAsync("rendervol@test.com", "pass1234", null);
        var client = await _factory.CreateAuthenticatedClientAsync();
        // Activate Spanish for this client (writes the culture cookie into the handler's container).
        await client.GetAsync("/set-language?culture=es-ES&returnUrl=/");

        var html = await client.GetStringAsync($"/volunteers/{id}");

        Assert.Contains("value=\"es-ES\" selected", html);
    }

    [Fact]
    public async Task VolunteerEdit_PreselectsSavedLanguage_OverActiveCulture()
    {
        var id = await SeedLoginVolunteerAsync("savedvol@test.com", "pass1234", "pt-BR");
        var client = await _factory.CreateAuthenticatedClientAsync();
        await client.GetAsync("/set-language?culture=es-ES&returnUrl=/");

        var html = await client.GetStringAsync($"/volunteers/{id}");

        Assert.Contains("value=\"pt-BR\" selected", html);
        Assert.DoesNotContain("value=\"es-ES\" selected", html);
    }

    [Fact]
    public async Task Logout_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/auth/logout");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.OriginalString ?? "");
    }
}
