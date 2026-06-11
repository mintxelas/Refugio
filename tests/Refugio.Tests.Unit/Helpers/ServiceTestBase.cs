using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application;
using Refugio.Domain.Common;
using Refugio.Infrastructure;
using Refugio.Infrastructure.Data;

namespace Refugio.Tests.Helpers;

/// <summary>
/// Wires the real application services + repositories + unit of work over a unique
/// in-memory EF database per test. Every service call runs in a fresh DI scope —
/// the same scope-per-request shape production has — so the change tracker never
/// leaks state between operations.
/// </summary>
public abstract class ServiceTestBase
{
    protected readonly IServiceScopeFactory ScopeFactory;

    protected ServiceTestBase()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ShelterDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddApplicationServices();
        services.AddInfrastructureServices();
        ConfigureServices(services);
        ScopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    /// <summary>Override to replace adapters (e.g. a capturing email sender).</summary>
    protected virtual void ConfigureServices(IServiceCollection services) { }

    /// <summary>Runs one service operation in its own scope (mirrors one HTTP request).</summary>
    protected async Task<T> WithServiceAsync<TService, T>(Func<TService, Task<T>> action) where TService : notnull
    {
        using var scope = ScopeFactory.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    protected async Task<T> SeedAsync<T>(Func<ShelterDbContext, T> seed) where T : class
    {
        using var scope = ScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        var entity = seed(db);
        await db.SaveChangesAsync();
        return entity;
    }

    /// <summary>Reads an entity by Id ignoring soft-delete filters, for asserting on DeletedAt.</summary>
    protected async Task<T?> ReadDirectAsync<T>(int id) where T : Entity
    {
        using var scope = ScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        return await db.Set<T>().IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
    }
}
