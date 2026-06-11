using Microsoft.Extensions.Caching.Memory;
using Refugio.Application.Contracts;

namespace Refugio.Web.Services;

/// <summary>
/// Caches the single ShelterSettings row so MainLayout does not hit the API on every
/// page render. Scoped (the API client needs the current request), but the cache itself
/// is the process-wide IMemoryCache. TTL is 60 s as a belt-and-suspenders expiry in case
/// invalidation is missed.
/// </summary>
public class SettingsCacheService(ShelterApiClient api, IMemoryCache cache, ILogger<SettingsCacheService> logger)
{
    private const string CacheKey = "shelter_settings";

    public async Task<ShelterSettingsDto?> GetSettingsAsync()
    {
        if (cache.TryGetValue(CacheKey, out ShelterSettingsDto? cached))
            return cached;

        try
        {
            var settings = await api.GetSettings();
            cache.Set(CacheKey, settings, TimeSpan.FromSeconds(60));
            return settings;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load shelter settings from the API");
            return null;
        }
    }

    public void Invalidate() => cache.Remove(CacheKey);
}
