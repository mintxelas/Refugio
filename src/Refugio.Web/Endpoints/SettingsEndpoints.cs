using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Web.Helpers;
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
            // PNG only: the resize is a fully-managed PNG pipeline (no GDI+, Linux-compatible),
            // and PNG carries the transparency the padded output needs.
            if (ext != ".png") return Results.Redirect("/settings?logoError=1");
            if (file.Length > 5 * 1024 * 1024) return Results.Redirect("/settings?logoError=1");
            var dir = Path.Combine(env.WebRootPath, "branding");
            Directory.CreateDirectory(dir);
            // Output is always a normalized 100x100 PNG, so remove any prior logo of any type.
            foreach (var old in Directory.GetFiles(dir, "logo.*")) File.Delete(old);
            const string fileName = "logo.png";
            try
            {
                await using var input = file.OpenReadStream();
                // Scale to the longest side (no distortion), center, pad the rest transparent → 100x100.
                ImageResizer.SaveSquareContainPng(input, Path.Combine(dir, fileName), 100);
            }
            catch (Exception)
            {
                // Unreadable / unsupported image content.
                return Results.Redirect("/settings?logoError=1");
            }
            // Cache-bust suffix: the filename is stable, so without ?v= a replacement
            // would reuse the URL and browsers could show the old image.
            await actors.Ask<bool>(new UpdateSettingsLogo($"/branding/{fileName}?v={DateTime.UtcNow.Ticks}"));
            settingsCache.Invalidate();
            return Results.Redirect("/settings");
        }).RequireAuthorization("Manager").DisableAntiforgery();

        return api;
    }
}
