using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Domain.Helpers;

namespace Refugio.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static void Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();

        SeedData.Seed(db);
        EnsureSeedAccount(db);
        NormalizeRoles(db);
    }

    private static void EnsureSeedAccount(ShelterDbContext db)
    {
        var elena = db.Volunteers.FirstOrDefault(v => v.Email == "elena@havensanctuary.org");
        if (elena is null || elena.CanLogin) return;
        elena.EnableLogin("shelter123");
        db.SaveChanges();
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
