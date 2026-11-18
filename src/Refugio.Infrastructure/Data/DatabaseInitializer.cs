using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Domain.Helpers;

namespace Refugio.Infrastructure.Data;

public static class DatabaseInitializer
{
    /// <param name="seedManagerPassword">Password for the seed manager on a fresh database. Required only when seeding.</param>
    public static void Initialize(IServiceProvider services, string? seedManagerPassword = null)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();

        SeedData.Seed(db, seedManagerPassword);
        NormalizeRoles(db);
    }

    private static void NormalizeRoles(ShelterDbContext db)
    {
        var changed = false;
        foreach (var v in db.Volunteers.ToList())
        {
            var normalized = v.Role is Roles.Manager or "Shelter Manager" ? Roles.Manager : Roles.Volunteer;
            if (v.Role != normalized) { v.ChangeRole(normalized); changed = true; }
        }
        if (changed) db.SaveChanges();
    }
}
