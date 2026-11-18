using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Refugio.Infrastructure.Data;

namespace Refugio.Tests.Integration;

/// <summary>
/// Own fixture: disables the seed manager's login, which would break other test classes.
/// </summary>
public class SeedAccountTests(ShelterWebFactory factory) : IClassFixture<ShelterWebFactory>
{
    private const string SeedEmail = "elena@havensanctuary.org";

    [Fact]
    public async Task Restart_DoesNotReEnableDisabledSeedLogin()
    {
        var manager = await factory.CreateAuthenticatedClientAsync();
        var volunteers = await manager.GetFromJsonAsync<List<JsonElement>>("/api/volunteers");
        var id = volunteers!.First(v => v.GetProperty("email").GetString() == SeedEmail).GetProperty("id").GetInt32();
        await manager.PutAsJsonAsync($"/api/volunteers/{id}", new
        {
            Id = id, Name = "Elena Smith", Email = SeedEmail, Phone = (string?)null, Role = "Manager",
            Notes = (string?)null, CanLogin = false, Status = "Active"
        });

        DatabaseInitializer.Initialize(factory.Services);

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await ShelterWebFactory.AttachCsrfTokenAsync(client);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = SeedEmail, password = "shelter123" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }
}
