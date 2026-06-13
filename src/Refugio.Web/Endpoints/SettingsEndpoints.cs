using Akka.Hosting;
using Refugio.Actors;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Web.Helpers;

namespace Refugio.Web.Endpoints;

public static class SettingsEndpoints
{
    public static RouteGroupBuilder MapSettingsEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/settings", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<SettingsActor>().AskRequired<ShelterSettingsDto>(new GetSettings(), ct)))
            .RequireAuthorization();

        api.MapPut("/settings", async (UpdateSettingsRequest request, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<SettingsActor>().AskRequired<ShelterSettingsDto>(request, ct)))
            .RequireAuthorization("Manager");

        api.MapPost("/settings/logo/upload", async (HttpContext ctx, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var file = ctx.Request.Form.Files.GetFile("Logo");
            if (file is null || file.Length == 0) return Results.BadRequest(new { error = "no_file" });
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".png") return Results.BadRequest(new { error = "png_only" });
            if (file.Length > 5 * 1024 * 1024) return Results.BadRequest(new { error = "too_large" });
            var dir = Path.Combine(env.WebRootPath, "branding");
            Directory.CreateDirectory(dir);
            foreach (var old in Directory.GetFiles(dir, "logo.*")) File.Delete(old);
            const string fileName = "logo.png";
            try
            {
                await using var input = file.OpenReadStream();
                ImageResizer.SaveSquareContainPng(input, Path.Combine(dir, fileName), 100);
            }
            catch (Exception)
            {
                return Results.BadRequest(new { error = "invalid_image" });
            }
            var url = $"/branding/{fileName}?v={DateTime.UtcNow.Ticks}";
            await actors.Get<SettingsActor>().AskRequired<bool>(new SetLogo(url), ct);
            return Results.Ok(new { url });
        }).RequireAuthorization("Manager").DisableAntiforgery();

        return api;
    }
}
