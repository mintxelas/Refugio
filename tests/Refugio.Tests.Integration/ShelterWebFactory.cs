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
        // Default suite creates many authenticated clients per test class; keep the auth
        // rate limiter effectively off here. AuthRateLimitTests overrides this back down
        // to exercise the real limit.
        builder.UseSetting("Auth:RateLimitPermitLimit", "1000");
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
        await AttachCsrfTokenAsync(client);
        await client.PostAsJsonAsync("/api/auth/login", new { email = "elena@havensanctuary.org", password = "shelter123" });
        // The antiforgery token embeds the caller's identity at issue time, so the anonymous
        // token fetched above no longer validates now that the client is authenticated.
        await AttachCsrfTokenAsync(client);
        return client;
    }

    /// <summary>
    /// Fetches the CSRF request token and sets it as a default header on the client, so every
    /// subsequent mutating call passes antiforgery validation. Call again after a login/logout
    /// on the same client — the token is bound to the caller's identity at issue time.
    /// </summary>
    public static async Task AttachCsrfTokenAsync(HttpClient client)
    {
        var token = await client.GetStringAsync("/api/antiforgery/token");
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
    }
}
