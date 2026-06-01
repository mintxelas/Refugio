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
        api.MapPut("/settings", async (UpdateSettings cmd, ShelterActorService actors) =>
            Results.Ok(await actors.Ask<ShelterSettings>(cmd)))
            .RequireAuthorization("Manager");

        // POST form handler — name/phrase update, redirects back to /settings
        api.MapPost("/settings/update", async (HttpContext ctx, ShelterActorService actors, SettingsCacheService settingsCache) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var name = form["Name"].FirstOrDefault() ?? "";
            var phrase = form["Phrase"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(name))
                return Results.Redirect("/settings?error=name");
            await actors.Ask<ShelterSettings>(new UpdateSettings(name, string.IsNullOrWhiteSpace(phrase) ? null : phrase));
            settingsCache.Invalidate();
            return Results.Redirect("/settings");
        }).RequireAuthorization("Manager");

        // Logo upload — multipart, mirrors /api/dogs/{id}/photo pattern
        api.MapPost("/settings/logo", async (HttpContext ctx, ShelterActorService actors, IWebHostEnvironment env, SettingsCacheService settingsCache) =>
        {
            var file = ctx.Request.Form.Files.GetFile("Logo");
            if (file is null || file.Length == 0) return Results.Redirect("/settings");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) return Results.Redirect("/settings");
            if (file.Length > 5 * 1024 * 1024) return Results.Redirect("/settings");
            var dir = Path.Combine(env.WebRootPath, "branding");
            Directory.CreateDirectory(dir);
            foreach (var old in Directory.GetFiles(dir, "logo.*")) File.Delete(old);
            var fileName = $"logo{ext}";
            await using var stream = File.Create(Path.Combine(dir, fileName));
            await file.CopyToAsync(stream);
            await actors.Ask<bool>(new UpdateSettingsLogo($"/branding/{fileName}"));
            settingsCache.Invalidate();
            return Results.Redirect("/settings");
        }).RequireAuthorization("Manager").DisableAntiforgery();

        return api;
    }
}
