using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure.Repositories;

public class SettingsRepository(ShelterDbContext db) : ISettingsRepository
{
    public Task<ShelterSettings?> GetAsync() => db.Settings.FirstOrDefaultAsync();

    public void Add(ShelterSettings settings) => db.Settings.Add(settings);
}
