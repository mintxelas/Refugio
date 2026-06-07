using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;

namespace Refugio.Web.Services;

/// <summary>
/// Caches the single ShelterSettings row so MainLayout does not hit the actor
/// system on every page render. The cache is invalidated when settings are updated.
/// TTL is 60 s as a belt-and-suspenders expiry in case invalidation is missed.
/// </summary>
public class SettingsCacheService(ShelterActorService actors, IMemoryCache cache, ILogger<SettingsCacheService> logger)
{
    private const string CacheKey = "shelter_settings";

    public async Task<ShelterSettings?> GetSettingsAsync()
    {
        if (cache.TryGetValue(CacheKey, out ShelterSettings? cached))
            return cached;

        try
        {
            var settings = await actors.Ask<ShelterSettings>(new GetSettings(), TimeSpan.FromSeconds(5));
            cache.Set(CacheKey, settings, TimeSpan.FromSeconds(60));
            return settings;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load shelter settings from actor system");
            return null;
        }
    }

    public void Invalidate() => cache.Remove(CacheKey);
}
