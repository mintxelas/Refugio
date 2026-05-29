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
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ShelterDbContext>));
            services.RemoveAll(typeof(ShelterDbContext));

            services.AddDbContext<ShelterDbContext>(opts =>
                opts.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
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
