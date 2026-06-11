using Refugio.Domain.Entities;

namespace Refugio.Domain.Repositories;

public interface ISettingsRepository
{
    /// <summary>The single settings row, or null when the shelter has none yet.</summary>
    Task<ShelterSettings?> GetAsync();
    void Add(ShelterSettings settings);
}
