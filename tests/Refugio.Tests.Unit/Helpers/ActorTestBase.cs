using Akka.TestKit.Xunit2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Domain.Entities;
using Refugio.Infrastructure.Data;

namespace Refugio.Tests.Helpers;

public abstract class ActorTestBase : TestKit
{
    protected readonly IServiceScopeFactory _sf;

    protected ActorTestBase()
    {
        var dbName = Guid.NewGuid().ToString(); // evaluate once; captured by lambda below
        var services = new ServiceCollection();
        services.AddDbContext<ShelterDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));
        ConfigureServices(services);
        _sf = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    protected virtual void ConfigureServices(IServiceCollection services) { }

    protected async Task<T> SeedAsync<T>(Func<ShelterDbContext, T> seed) where T : class
    {
        using var scope = _sf.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        var entity = seed(db);
        await db.SaveChangesAsync();
        return entity;
    }

    /// <summary>Reads entity by Id ignoring soft-delete global query filters, for asserting on DeletedAt.</summary>
    protected async Task<T?> ReadDirectAsync<T>(int id) where T : class, ISoftDeletable
    {
        using var scope = _sf.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        return await db.Set<T>().IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
    }

    // Seed two related entities: first is saved independently to get its Id, second references it.
    protected async Task<(T1, T2)> SeedRelatedAsync<T1, T2>(
        Func<ShelterDbContext, T1> seedFirst,
        Func<ShelterDbContext, T1, T2> seedSecond)
        where T1 : class
        where T2 : class
    {
        using var scope = _sf.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        var first = seedFirst(db);
        await db.SaveChangesAsync();
        var second = seedSecond(db, first);
        await db.SaveChangesAsync();
        return (first, second);
    }
}
