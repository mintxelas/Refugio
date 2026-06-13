using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Refugio.Infrastructure.Data;

namespace Refugio.Tests.Integration;

public class ShelterWebFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString =
        $"Data Source=RefugioTests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private SqliteConnection? _keeperConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _keeperConnection = new SqliteConnection(_connectionString);
        _keeperConnection.Open();

        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ShelterDbContext>));
            services.RemoveAll(typeof(ShelterDbContext));

            services.AddDbContext<ShelterDbContext>(opts =>
                opts.UseSqlite(_connectionString));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _keeperConnection?.Dispose();
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.PostAsJsonAsync("/api/auth/login", new { email = "elena@havensanctuary.org", password = "shelter123" });
        return client;
    }
}
