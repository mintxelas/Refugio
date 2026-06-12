using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Refugio.Application.Contracts;
using Refugio.Domain.Entities;

namespace Refugio.Tests.Integration;

public class SettingsApiTests : IClassFixture<ShelterWebFactory>
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ShelterWebFactory _factory;

    public SettingsApiTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AnonClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> ManagerClientAsync() =>
        await _factory.CreateAuthenticatedClientAsync();

    private async Task<HttpClient> VolunteerClientAsync()
    {
        // Create a volunteer user and log in as them
        var mgr = await ManagerClientAsync();
        var resp = await mgr.PostAsJsonAsync("/api/volunteers", new
        {
            Name = "SettingsTestVolunteer",
            Email = "settingsvolunteer@test.com",
            Phone = (string?)null,
            Role = "Volunteer",
            Notes = (string?)null,
            CanLogin = true,
            Password = "test1234"
        });
        // If already exists (shared factory), just log in
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "settingsvolunteer@test.com",
            ["password"] = "test1234"
        });
        await client.PostAsync("/auth/login", form);
        return client;
    }

    [Fact]
    public async Task GetSettings_Authenticated_ReturnsOk()
    {
        var client = await ManagerClientAsync();
        var response = await client.GetAsync("/api/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<ShelterSettingsDto>(_jsonOpts);
        Assert.NotNull(settings);
        Assert.NotEmpty(settings.Name);
    }

    [Fact]
    public async Task GetSettings_Anon_RedirectsToLogin()
    {
        var response = await AnonClient().GetAsync("/api/settings");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task UpdateSettings_AsManager_UpdatesNameAndPhrase()
    {
        var client = await ManagerClientAsync();
        var response = await client.PutAsJsonAsync("/api/settings", new
        {
            Name = "SettingsTest Shelter",
            Phrase = "Test Branch"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ShelterSettingsDto>(_jsonOpts);
        Assert.NotNull(updated);
        Assert.Equal("SettingsTest Shelter", updated.Name);
        Assert.Equal("Test Branch", updated.Phrase);
    }

    [Fact]
    public async Task UpdateSettings_AsAnon_Returns401()
    {
        var response = await AnonClient().PutAsJsonAsync("/api/settings", new
        {
            Name = "ShouldFail",
            Phrase = (string?)null
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogoUpload_AsManager_StoresFileAndUpdatesUrl()
    {
        var client = await ManagerClientAsync();

        // Minimal valid PNG (1x1 pixel)
        var pngBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR length + type
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // width=1, height=1
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, // bit depth, color type, etc.
            0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT length + type
            0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00, // compressed data
            0x00, 0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, // CRC
            0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, // IEND length + type
            0x44, 0xAE, 0x42, 0x60, 0x82                    // IEND data + CRC
        };

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pngBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "Logo", "test-logo.png");

        var response = await client.PostAsync("/api/settings/logo", content);
        // DisableAntiforgery endpoint redirects on success
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/settings", response.Headers.Location?.OriginalString);

        // Verify the URL was persisted
        var getResponse = await client.GetAsync("/api/settings");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var settings = await getResponse.Content.ReadFromJsonAsync<ShelterSettingsDto>(_jsonOpts);
        Assert.NotNull(settings?.LogoUrl);
        Assert.Contains("/branding/", settings.LogoUrl);
    }

    [Fact]
    public async Task LogoUpload_InvalidExtension_RedirectsWithoutUpdate()
    {
        var client = await ManagerClientAsync();

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("not an image"));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "Logo", "bad-file.txt");

        var response = await client.PostAsync("/api/settings/logo", content);
        // Should still redirect (back to /settings) but not crash
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    }

    [Fact]
    public async Task LogoUpload_AsAnon_RedirectsToLogin()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        content.Add(fileContent, "Logo", "test.png");
        var response = await AnonClient().PostAsync("/api/settings/logo", content);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task SettingsPage_RendersForManager()
    {
        var client = await ManagerClientAsync();
        var response = await client.GetAsync("/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("branding", html);
    }

    [Fact]
    public async Task SettingsPage_AsAnon_RedirectsToLogin()
    {
        var response = await AnonClient().GetAsync("/settings");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }
}
