using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Contracts;
using Refugio.Application.Services;
using Refugio.Infrastructure.Data;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Services;

public class SeedDataTests : ServiceTestBase
{
    [Fact]
    public void Seed_FreshDatabaseWithoutManagerPassword_Throws()
    {
        using var scope = ScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

        Assert.Throws<InvalidOperationException>(() => SeedData.Seed(db, null));
    }

    [Fact]
    public async Task Seed_WithManagerPassword_ManagerCanLogInWithIt()
    {
        using (var scope = ScopeFactory.CreateScope())
            SeedData.Seed(scope.ServiceProvider.GetRequiredService<ShelterDbContext>(), "configured-secret");

        var login = await WithServiceAsync<IVolunteerService, VolunteerDto?>(
            s => s.LoginAsync("elena@havensanctuary.org", "configured-secret"));

        Assert.NotNull(login);
    }
}
