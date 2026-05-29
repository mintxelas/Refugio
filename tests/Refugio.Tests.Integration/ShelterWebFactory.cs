using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Infrastructure.Data;

namespace Refugio.Tests.Integration;

public class ShelterWebFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"ShelterTest_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ShelterDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ShelterDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName).EnableServiceProviderCaching(false));
        });
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
