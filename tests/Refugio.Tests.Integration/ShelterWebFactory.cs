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
    // Shared-cache named in-memory DB: every DbContext opens its own connection to the
    // same database, so the SSR pages' parallel API calls don't fight over one physical
    // connection (single-connection ":memory:" throws 'database is locked' under
    // concurrency). The keeper connection holds the database alive for the factory's life.
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

            // The SSR UI talks to the API through the named HttpClient. TestServer has no
            // real socket, so route that client through the in-memory test handler.
            // (Resolved lazily at first use — the host is running by then.)
            services.AddHttpClient(Refugio.Web.Services.ShelterApiClient.ClientName)
                .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());
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
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "elena@havensanctuary.org",
            ["password"] = "shelter123"
        });
        await client.PostAsync("/auth/login", form);
        return client;
    }
}
