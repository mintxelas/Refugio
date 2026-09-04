using Akka.Hosting;
using Refugio.Actors;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Queries;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Web.Helpers;

namespace Refugio.Web.Endpoints;

public static class DogEndpoints
{
    public static RouteGroupBuilder MapDogEndpoints(this RouteGroupBuilder api)
    {
        // Dashboard
        api.MapGet("/dashboard", async (IDashboardQueries dashboard) =>
            Results.Ok(await dashboard.GetStatsAsync())).AllowAnonymous();

        // Upcoming vet visits (used by the Health page in the React SPA)
        api.MapGet("/reports/upcoming-visits", async (int? daysAhead, IMedicalQueries medicalQueries) =>
            Results.Ok(await medicalQueries.GetUpcomingVisitsAsync(daysAhead ?? 7)))
            .RequireAuthorization();

        // Dogs needing urgent medication attention (used by the Health page's urgent-meds drill-down)
        api.MapGet("/reports/urgent-medications", async (IDashboardQueries dashboard) =>
            Results.Ok(await dashboard.GetUrgentMedicationsAsync()))
            .RequireAuthorization();

        // Dogs
        api.MapGet("/dogs", async (string? search, DogStatus? status, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<DogActor>().AskRequired<List<DogDto>>(new GetDogs(search, status), ct))).AllowAnonymous();

        api.MapGet("/dogs/paged", async (string? search, DogStatus? status, int? page, int? pageSize, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<DogActor>().AskRequired<Page<DogDto>>(new GetDogsPaged(search, status, page ?? 1, pageSize ?? 20), ct))).AllowAnonymous();

        api.MapGet("/dogs/deleted", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<DogActor>().AskRequired<List<DogDto>>(new GetDeletedDogs(), ct)))
            .RequireAuthorization("Manager");

        api.MapGet("/dogs/deleted/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var dog = await actors.Get<DogActor>().AskFor<DogDto>(new GetDeletedDog(id), ct);
            return dog is null ? Results.NotFound() : Results.Ok(dog);
        }).RequireAuthorization("Manager");

        api.MapGet("/dogs/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var dog = await actors.Get<DogActor>().AskFor<DogDto>(new GetDog(id), ct);
            return dog is null ? Results.NotFound() : Results.Ok(dog);
        }).AllowAnonymous();

        api.MapPost("/dogs", async (CreateDogRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var dog = await actors.Get<DogActor>().AskRequired<DogDto>(request, ct);
            return Results.Created($"/api/dogs/{dog.Id}", dog);
        });

        api.MapPut("/dogs/{id:int}", async (int id, UpdateDogRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var dog = await actors.Get<DogActor>().AskFor<DogDto>(request with { Id = id }, ct);
            return dog is null ? Results.NotFound() : Results.Ok(dog);
        });

        api.MapDelete("/dogs/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<DogActor>().AskRequired<bool>(new DeleteDog(id), ct)
                ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("Manager");

        api.MapPost("/dogs/{id:int}/delete", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<DogActor>().AskRequired<bool>(new DeleteDog(id), ct);
            return Results.Redirect("/dogs");
        }).RequireAuthorization("Manager");

        api.MapPost("/dogs/{id:int}/restore", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<DogActor>().AskRequired<bool>(new RestoreDog(id), ct);
            return Results.Redirect("/admin/deleted?tab=dogs");
        }).RequireAuthorization("Manager");

        api.MapPost("/dogs/{id:int}/purge", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<DogActor>().AskRequired<bool>(new PurgeDog(id), ct);
            return Results.Redirect("/admin/deleted?tab=dogs");
        }).RequireAuthorization("Manager");

        api.MapPost("/dogs/{id:int}/photo", async (int id, HttpContext ctx, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var file = ctx.Request.Form.Files.GetFile("Photo");
            if (file is null || file.Length == 0) return Results.Redirect($"/dogs/{id}/edit");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!PhotoFiles.IsImageExtension(ext)) return Results.Redirect($"/dogs/{id}/edit");
            if (file.Length > 2 * 1024 * 1024) return Results.Redirect($"/dogs/{id}/edit");
            if (!PhotoFiles.HasImageBytes(file)) return Results.Redirect($"/dogs/{id}/edit");
            var dir = Path.Combine(env.WebRootPath, "photos", "dogs", id.ToString());
            Directory.CreateDirectory(dir);
            foreach (var old in Directory.GetFiles(dir, "primary.*")) File.Delete(old);
            var fileName = $"primary{ext}";
            await using var stream = File.Create(Path.Combine(dir, fileName));
            await file.CopyToAsync(stream);
            await actors.Get<DogActor>().AskRequired<bool>(new SetDogPhoto(id, $"/photos/dogs/{id}/{fileName}"), ct);
            return Results.Redirect($"/dogs/{id}/edit");
        }).RequireAuthorization().DisableAntiforgery();

        // Dog photo gallery — multiple images per dog, one marked default
        api.MapGet("/dogs/{id:int}/photos", async (int id, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<DogActor>().AskRequired<List<DogPhotoDto>>(new GetDogPhotos(id), ct))).AllowAnonymous();

        api.MapPost("/dogs/{id:int}/photos", async (int id, HttpContext ctx, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var dir = Path.Combine(env.WebRootPath, "photos", "dogs", id.ToString());
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
                await actors.Get<DogActor>().AskFor<DogPhotoDto>(new AddDogPhoto(id, $"/photos/dogs/{id}/{fileName}"), ct);
            }
            return Results.Redirect($"/dogs/{id}/edit");
        }).RequireAuthorization().DisableAntiforgery();

        // JSON upload variant for the React SPA — returns { urls: [...] } instead of redirecting.
        api.MapPost("/dogs/{id:int}/photos/upload", async (int id, HttpContext ctx, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var dir = Path.Combine(env.WebRootPath, "photos", "dogs", id.ToString());
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
                var url = $"/photos/dogs/{id}/{fileName}";
                await actors.Get<DogActor>().AskFor<DogPhotoDto>(new AddDogPhoto(id, url), ct);
                uploaded.Add(url);
            }
            return uploaded.Count == 0
                ? Results.BadRequest(new { error = "no_valid_files" })
                : Results.Ok(new { urls = uploaded });
        }).RequireAuthorization().DisableAntiforgery();

        api.MapPost("/dogs/photos/{photoId:int}/default", async (int photoId, int dogId, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<DogActor>().AskRequired<bool>(new SetDefaultDogPhoto(photoId), ct);
            return Results.Redirect($"/dogs/{dogId}/edit");
        }).RequireAuthorization();

        api.MapPost("/dogs/photos/{photoId:int}/delete", async (int photoId, int dogId, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var url = await actors.Get<DogActor>().AskFor<string>(new RemoveDogPhoto(photoId), ct);
            PhotoFiles.DeleteByUrl(env, url);
            return Results.Redirect($"/dogs/{dogId}/edit");
        }).RequireAuthorization();

        // Medical records
        api.MapGet("/dogs/{id:int}/medical", async (int id, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<DogActor>().AskRequired<List<MedicalRecordDto>>(new GetMedicalRecords(id), ct)));

        api.MapPost("/dogs/{id:int}/medical", async (int id, CreateMedicalRecordRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var record = await actors.Get<DogActor>().AskFor<MedicalRecordDto>(request with { DogId = id }, ct);
            return record is null
                ? Results.NotFound()
                : Results.Created($"/api/dogs/{id}/medical/{record.Id}", record);
        });

        api.MapGet("/dogs/{id:int}/medical/{recId:int}", async (int recId, IActorRegistry actors, CancellationToken ct) =>
        {
            var record = await actors.Get<DogActor>().AskFor<MedicalRecordDto>(new GetMedicalRecord(recId), ct);
            return record is null ? Results.NotFound() : Results.Ok(record);
        });

        api.MapGet("/medical/deleted", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<DogActor>().AskRequired<List<MedicalRecordDto>>(new GetDeletedMedicalRecords(), ct)))
            .RequireAuthorization("Manager");

        api.MapGet("/medical/deleted/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var record = await actors.Get<DogActor>().AskFor<MedicalRecordDto>(new GetDeletedMedicalRecord(id), ct);
            return record is null ? Results.NotFound() : Results.Ok(record);
        }).RequireAuthorization("Manager");

        api.MapGet("/medical/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var record = await actors.Get<DogActor>().AskFor<MedicalRecordDto>(new GetMedicalRecord(id), ct);
            return record is null ? Results.NotFound() : Results.Ok(record);
        });

        api.MapPut("/medical/{id:int}", async (int id, UpdateMedicalRecordRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var record = await actors.Get<DogActor>().AskFor<MedicalRecordDto>(request with { Id = id }, ct);
            return record is null ? Results.NotFound() : Results.Ok(record);
        });

        api.MapDelete("/medical/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<DogActor>().AskRequired<bool>(new DeleteMedicalRecord(id), ct)
                ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("Manager");

        api.MapPost("/medical/{id:int}/delete", async (int id, int? dogId, string? returnUrl, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<DogActor>().AskRequired<bool>(new DeleteMedicalRecord(id), ct);
            var dest = returnUrl is not null && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
                ? returnUrl
                : (dogId.HasValue ? $"/dogs/{dogId}" : "/dogs");
            return Results.Redirect(dest);
        }).RequireAuthorization("Manager");

        api.MapPost("/medical/{id:int}/restore", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<DogActor>().AskRequired<bool>(new RestoreMedicalRecord(id), ct);
            return Results.Redirect("/admin/deleted?tab=medical");
        }).RequireAuthorization("Manager");

        api.MapPost("/medical/{id:int}/purge", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<DogActor>().AskRequired<bool>(new PurgeMedicalRecord(id), ct);
            return Results.Redirect("/admin/deleted?tab=medical");
        }).RequireAuthorization("Manager");

        // Medications
        api.MapGet("/dogs/{id:int}/medications", async (int id, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<DogActor>().AskRequired<List<MedicationDto>>(new GetMedications(id), ct)));

        api.MapPost("/dogs/{id:int}/medications", async (int id, CreateMedicationRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var medication = await actors.Get<DogActor>().AskFor<MedicationDto>(request with { DogId = id }, ct);
            return medication is null
                ? Results.NotFound()
                : Results.Created($"/api/dogs/{id}/medications/{medication.Id}", medication);
        });

        api.MapGet("/medications/deleted", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<DogActor>().AskRequired<List<MedicationDto>>(new GetDeletedMedications(), ct)))
            .RequireAuthorization("Manager");

        api.MapGet("/medications/deleted/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var medication = await actors.Get<DogActor>().AskFor<MedicationDto>(new GetDeletedMedication(id), ct);
            return medication is null ? Results.NotFound() : Results.Ok(medication);
        }).RequireAuthorization("Manager");

        api.MapGet("/medications/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var medication = await actors.Get<DogActor>().AskFor<MedicationDto>(new GetMedication(id), ct);
            return medication is null ? Results.NotFound() : Results.Ok(medication);
        });

        api.MapPut("/medications/{id:int}", async (int id, UpdateMedicationRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var medication = await actors.Get<DogActor>().AskFor<MedicationDto>(request with { Id = id }, ct);
            return medication is null ? Results.NotFound() : Results.Ok(medication);
        });

        api.MapPost("/medications/{id:int}/delete", async (int id, int? dogId, string? returnUrl, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<DogActor>().AskRequired<bool>(new DeleteMedication(id), ct);
            var dest = returnUrl is not null && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
                ? returnUrl
                : (dogId.HasValue ? $"/dogs/{dogId}" : "/dogs");
            return Results.Redirect(dest);
        }).RequireAuthorization("Manager");

        api.MapPost("/medications/{id:int}/restore", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<DogActor>().AskRequired<bool>(new RestoreMedication(id), ct);
            return Results.Redirect("/admin/deleted?tab=medications");
        }).RequireAuthorization("Manager");

        api.MapPost("/medications/{id:int}/purge", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<DogActor>().AskRequired<bool>(new PurgeMedication(id), ct);
            return Results.Redirect("/admin/deleted?tab=medications");
        }).RequireAuthorization("Manager");

        // Kept verb: external consumers "delete" a medication by deactivating the course.
        api.MapDelete("/medications/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<DogActor>().AskRequired<bool>(new DeactivateMedication(id), ct)
                ? Results.NoContent() : Results.NotFound());

        return api;
    }
}
