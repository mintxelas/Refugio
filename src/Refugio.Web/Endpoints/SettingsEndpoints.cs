using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Web.Services;

namespace Refugio.Web.Endpoints;

public static class SettingsEndpoints
{
    public static RouteGroupBuilder MapSettingsEndpoints(this RouteGroupBuilder api)
    {
        // REST read
        api.MapGet("/settings", async (ShelterActorService actors) =>
            Results.Ok(await actors.Ask<ShelterSettings>(new GetSettings())))
            .RequireAuthorization();

        // REST update (for external consumers)
        api.MapPut("/settings", async (UpdateSettings cmd, ShelterActorService actors, SettingsCacheService settingsCache) =>
        {
            var updated = await actors.Ask<ShelterSettings>(cmd);
            settingsCache.Invalidate();
            return Results.Ok(updated);
        }).RequireAuthorization("Manager");

        // Logo upload — multipart, mirrors /api/dogs/{id}/photo pattern
        api.MapPost("/settings/logo", async (HttpContext ctx, ShelterActorService actors, IWebHostEnvironment env, SettingsCacheService settingsCache) =>
        {
            var file = ctx.Request.Form.Files.GetFile("Logo");
            if (file is null || file.Length == 0) return Results.Redirect("/settings?logoError=1");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) return Results.Redirect("/settings?logoError=1");
            if (file.Length > 5 * 1024 * 1024) return Results.Redirect("/settings?logoError=1");
            var dir = Path.Combine(env.WebRootPath, "branding");
            Directory.CreateDirectory(dir);
            foreach (var old in Directory.GetFiles(dir, "logo.*")) File.Delete(old);
            var fileName = $"logo{ext}";
            await using var stream = File.Create(Path.Combine(dir, fileName));
            await file.CopyToAsync(stream);
            // Cache-bust suffix: the filename is stable, so without ?v= a same-type
            // replacement would reuse the URL and browsers could show the old image.
            await actors.Ask<bool>(new UpdateSettingsLogo($"/branding/{fileName}?v={DateTime.UtcNow.Ticks}"));
            settingsCache.Invalidate();
            return Results.Redirect("/settings");
        }).RequireAuthorization("Manager").DisableAntiforgery();

        return api;
    }
}
