using Akka.Hosting;
using Refugio.Actors;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Queries;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Web.Helpers;

namespace Refugio.Web.Endpoints;

public static class AdoptionEndpoints
{
    public static RouteGroupBuilder MapAdoptionEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/adoptions", async (AdoptionStatus? status, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<AdoptionActor>().AskRequired<List<AdoptionDto>>(new GetAdoptions(status), ct)));

        api.MapGet("/adoptions/paged", async (AdoptionStatus? status, int? page, int? pageSize, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<AdoptionActor>().AskRequired<Page<AdoptionDto>>(new GetAdoptionsPaged(status, page ?? 1, pageSize ?? 25), ct)));

        api.MapGet("/adoptions/deleted", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<AdoptionActor>().AskRequired<List<AdoptionDto>>(new GetDeletedAdoptions(), ct)))
            .RequireAuthorization("Manager");

        api.MapGet("/adoptions/deleted/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var adoption = await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(new GetDeletedAdoption(id), ct);
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        }).RequireAuthorization("Manager");

        api.MapGet("/adoptions/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var adoption = await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(new GetAdoption(id), ct);
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        });

        api.MapPost("/adoptions", async (CreateAdoptionRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var adoption = await actors.Get<AdoptionActor>().AskRequired<AdoptionDto>(request, ct);
            return Results.Created($"/api/adoptions/{adoption.Id}", adoption);
        });

        api.MapPut("/adoptions/{id:int}", async (int id, UpdateAdoptionRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var adoption = await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(request with { Id = id }, ct);
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        });

        api.MapPut("/adoptions/{id:int}/status", async (int id, UpdateAdoptionStatusRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var adoption = await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(request with { Id = id }, ct);
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        });

        api.MapDelete("/adoptions/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<AdoptionActor>().AskRequired<bool>(new DeleteAdoption(id), ct)
                ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/delete", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<AdoptionActor>().AskRequired<bool>(new DeleteAdoption(id), ct);
            return Results.Redirect("/adoptions");
        }).RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/restore", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<AdoptionActor>().AskRequired<bool>(new RestoreAdoption(id), ct);
            return Results.Redirect("/admin/deleted?tab=adoptions");
        }).RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/purge", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<AdoptionActor>().AskRequired<bool>(new PurgeAdoption(id), ct);
            return Results.Redirect("/admin/deleted?tab=adoptions");
        }).RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/advance", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(new AdvanceAdoption(id), ct);
            return Results.Redirect("/adoptions");
        }).RequireAuthorization();

        api.MapPost("/adoptions/{id:int}/reject", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(
                new UpdateAdoptionStatusRequest(id, AdoptionStatus.Rejected, null), ct);
            return Results.Redirect("/adoptions");
        }).RequireAuthorization();

        // Adoption photo gallery
        api.MapGet("/adoptions/{id:int}/photos", async (int id, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<AdoptionActor>().AskRequired<List<AdoptionPhotoDto>>(new GetAdoptionPhotos(id), ct)));

        api.MapPost("/adoptions/{id:int}/photos", async (int id, HttpContext ctx, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var dir = Path.Combine(env.WebRootPath, "photos", "adoption", id.ToString());
            Directory.CreateDirectory(dir);
            foreach (var file in ctx.Request.Form.Files.GetFiles("Photos"))
            {
                if (file.Length == 0 || file.Length > 2 * 1024 * 1024) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!PhotoFiles.IsImageExtension(ext)) continue;
                if (!PhotoFiles.HasImageBytes(file)) continue;
                var fileName = $"{Guid.NewGuid():N}{ext}";
                await using var stream = File.Create(Path.Combine(dir, fileName));
                await file.CopyToAsync(stream);
                await actors.Get<AdoptionActor>().AskFor<AdoptionPhotoDto>(new AddAdoptionPhoto(id, $"/photos/adoption/{id}/{fileName}"), ct);
            }
            return Results.Redirect($"/adoptions/{id}");
        }).RequireAuthorization().DisableAntiforgery();

        api.MapPost("/adoptions/{id:int}/photos/upload", async (int id, HttpContext ctx, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var dir = Path.Combine(env.WebRootPath, "photos", "adoption", id.ToString());
            Directory.CreateDirectory(dir);
            var uploaded = new List<string>();
            foreach (var file in ctx.Request.Form.Files.GetFiles("Photos"))
            {
                if (file.Length == 0 || file.Length > 2 * 1024 * 1024) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!PhotoFiles.IsImageExtension(ext)) continue;
                if (!PhotoFiles.HasImageBytes(file)) continue;
                var fileName = $"{Guid.NewGuid():N}{ext}";
                await using var stream = File.Create(Path.Combine(dir, fileName));
                await file.CopyToAsync(stream);
                var url = $"/photos/adoption/{id}/{fileName}";
                await actors.Get<AdoptionActor>().AskFor<AdoptionPhotoDto>(new AddAdoptionPhoto(id, url), ct);
                uploaded.Add(url);
            }
            return uploaded.Count == 0
                ? Results.BadRequest(new { error = "no_valid_files" })
                : Results.Ok(new { urls = uploaded });
        }).RequireAuthorization().DisableAntiforgery();

        api.MapPost("/adoptions/photos/{photoId:int}/delete", async (int photoId, int adoptionId, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var url = await actors.Get<AdoptionActor>().AskFor<string>(new RemoveAdoptionPhoto(photoId), ct);
            PhotoFiles.DeleteByUrl(env, url);
            return Results.Redirect($"/adoptions/{adoptionId}");
        }).RequireAuthorization();

        // Reports (sourced from adoption data) — CQRS read models stay direct.
        api.MapGet("/reports/adoption-conversion", async (int? year, IAdoptionQueries reports) =>
            Results.Ok(await reports.GetConversionStatsAsync(year ?? DateTime.UtcNow.Year)))
            .RequireAuthorization();

        api.MapGet("/reports/shelter-stay", async (IAdoptionQueries reports) =>
            Results.Ok(await reports.GetShelterStayStatsAsync()))
            .RequireAuthorization();

        return api;
    }
}
