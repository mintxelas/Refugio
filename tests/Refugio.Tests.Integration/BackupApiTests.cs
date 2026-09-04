using System.IO.Compression;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

public class BackupApiTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public BackupApiTests(ShelterWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Backup_AsManager_ReturnsZipContainingDatabase()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/settings/backup");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var db = zip.GetEntry("shelter.db");
        Assert.NotNull(db);
        Assert.True(db!.Length > 0);
    }

    [Fact]
    public async Task Backup_AsAnon_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/settings/backup");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }
}
