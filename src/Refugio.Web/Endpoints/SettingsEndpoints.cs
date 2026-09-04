using System.IO.Compression;
using Akka.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Refugio.Actors;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Infrastructure.Data;
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

        api.MapGet("/settings/backup", async (ShelterDbContext db, IWebHostEnvironment env, HttpContext ctx, CancellationToken ct) =>
        {
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

            // ponytail: buffer the zip in memory (ZipArchive writes synchronously, Kestrel forbids sync IO
            // on the response body). Fine for a shelter's data volume; stream to a temp file if it ever grows large.
            var buffer = new MemoryStream();
            using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                // Database: consistent snapshot via SQLite online-backup API (the live DB may be mid-write).
                var dbTmp = Path.Combine(Path.GetTempPath(), $"refugio-backup-{stamp}.db");
                try
                {
                    await using (var src = new SqliteConnection(db.Database.GetConnectionString()))
                    await using (var dst = new SqliteConnection($"Data Source={dbTmp};Pooling=False"))
                    {
                        await src.OpenAsync(ct);
                        await dst.OpenAsync(ct);
                        src.BackupDatabase(dst);
                    }
                    zip.CreateEntryFromFile(dbTmp, "shelter.db");
                }
                finally
                {
                    if (File.Exists(dbTmp)) File.Delete(dbTmp);
                }

                // Uploaded pictures.
                foreach (var sub in string.IsNullOrEmpty(env.WebRootPath) ? Array.Empty<string>() : new[] { "photos", "branding" })
                {
                    var root = Path.Combine(env.WebRootPath!, sub);
                    if (!Directory.Exists(root)) continue;
                    foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                        zip.CreateEntryFromFile(file, Path.GetRelativePath(env.WebRootPath!, file).Replace('\\', '/'));
                }
            }

            buffer.Position = 0;
            ctx.Response.Headers.ContentDisposition = $"attachment; filename=refugio-backup-{stamp}.zip";
            return Results.File(buffer, "application/zip");
        }).RequireAuthorization("Manager");

        return api;
    }
}
