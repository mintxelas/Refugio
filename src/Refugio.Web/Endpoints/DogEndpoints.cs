using Refugio.Application.Contracts;
using Refugio.Application.Queries;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Web.Helpers;

namespace Refugio.Web.Endpoints;

public static class DogEndpoints
{
    public static RouteGroupBuilder MapDogEndpoints(this RouteGroupBuilder api)
    {
        // Dashboard
        api.MapGet("/dashboard", async (IDashboardQueries dashboard) =>
            Results.Ok(await dashboard.GetStatsAsync()));

        // Dogs
        api.MapGet("/dogs", async (string? search, DogStatus? status, IDogService dogs) =>
            Results.Ok(await dogs.GetDogsAsync(search, status)));

        api.MapGet("/dogs/paged", async (string? search, DogStatus? status, int? page, int? pageSize, IDogService dogs) =>
            Results.Ok(await dogs.GetDogsPagedAsync(search, status, page ?? 1, pageSize ?? 20)));

        api.MapGet("/dogs/deleted", async (IDogService dogs) =>
            Results.Ok(await dogs.GetDeletedDogsAsync())).RequireAuthorization("Manager");

        api.MapGet("/dogs/deleted/{id:int}", async (int id, IDogService dogs) =>
        {
            var dog = await dogs.GetDeletedDogAsync(id);
            return dog is null ? Results.NotFound() : Results.Ok(dog);
        }).RequireAuthorization("Manager");

        api.MapGet("/dogs/{id:int}", async (int id, IDogService dogs) =>
        {
            var dog = await dogs.GetDogAsync(id);
            return dog is null ? Results.NotFound() : Results.Ok(dog);
        });

        api.MapPost("/dogs", async (CreateDogRequest request, IDogService dogs) =>
        {
            var dog = await dogs.CheckInDogAsync(request);
            return Results.Created($"/api/dogs/{dog.Id}", dog);
        });

        api.MapPut("/dogs/{id:int}", async (int id, UpdateDogRequest request, IDogService dogs) =>
        {
            var dog = await dogs.UpdateDogAsync(request with { Id = id });
            return dog is null ? Results.NotFound() : Results.Ok(dog);
        });

        api.MapDelete("/dogs/{id:int}", async (int id, IDogService dogs) =>
            await dogs.DeleteDogAsync(id) ? Results.NoContent() : Results.NotFound());

        api.MapPost("/dogs/{id:int}/delete", async (int id, IDogService dogs) =>
        {
            await dogs.DeleteDogAsync(id);
            return Results.Redirect("/dogs");
        }).RequireAuthorization("Manager");

        api.MapPost("/dogs/{id:int}/restore", async (int id, IDogService dogs) =>
        {
            await dogs.RestoreDogAsync(id);
            return Results.Redirect("/admin/deleted?tab=dogs");
        }).RequireAuthorization("Manager");

        api.MapPost("/dogs/{id:int}/purge", async (int id, IDogService dogs) =>
        {
            await dogs.PurgeDogAsync(id);
            return Results.Redirect("/admin/deleted?tab=dogs");
        }).RequireAuthorization("Manager");

        api.MapPost("/dogs/{id:int}/photo", async (int id, HttpContext ctx, IDogService dogs, IWebHostEnvironment env) =>
        {
            var file = ctx.Request.Form.Files.GetFile("Photo");
            if (file is null || file.Length == 0) return Results.Redirect($"/dogs/{id}/edit");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) return Results.Redirect($"/dogs/{id}/edit");
            if (file.Length > 5 * 1024 * 1024) return Results.Redirect($"/dogs/{id}/edit");
            var dir = Path.Combine(env.WebRootPath, "dogs");
            Directory.CreateDirectory(dir);
            foreach (var old in Directory.GetFiles(dir, $"{id}.*")) File.Delete(old);
            var fileName = $"{id}{ext}";
            await using var stream = File.Create(Path.Combine(dir, fileName));
            await file.CopyToAsync(stream);
            await dogs.SetDogPhotoAsync(id, $"/dogs/{fileName}");
            return Results.Redirect($"/dogs/{id}/edit");
        }).RequireAuthorization().DisableAntiforgery();

        // Dog photo gallery — multiple images per dog, one marked default
        api.MapGet("/dogs/{id:int}/photos", async (int id, IDogService dogs) =>
            Results.Ok(await dogs.GetDogPhotosAsync(id)));

        api.MapPost("/dogs/{id:int}/photos", async (int id, HttpContext ctx, IDogService dogs, IWebHostEnvironment env) =>
        {
            var dir = Path.Combine(env.WebRootPath, "dogs");
            Directory.CreateDirectory(dir);
            foreach (var file in ctx.Request.Form.Files.GetFiles("Photos"))
            {
                if (file.Length == 0 || file.Length > 5 * 1024 * 1024) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) continue;
                var fileName = $"{id}_{Guid.NewGuid():N}{ext}";
                await using var stream = File.Create(Path.Combine(dir, fileName));
                await file.CopyToAsync(stream);
                await dogs.AddDogPhotoAsync(id, $"/dogs/{fileName}");
            }
            return Results.Redirect($"/dogs/{id}/edit");
        }).RequireAuthorization().DisableAntiforgery();

        api.MapPost("/dogs/photos/{photoId:int}/default", async (int photoId, int dogId, IDogService dogs) =>
        {
            await dogs.SetDefaultDogPhotoAsync(photoId);
            return Results.Redirect($"/dogs/{dogId}/edit");
        }).RequireAuthorization();

        api.MapPost("/dogs/photos/{photoId:int}/delete", async (int photoId, int dogId, IDogService dogs, IWebHostEnvironment env) =>
        {
            var url = await dogs.RemoveDogPhotoAsync(photoId);
            PhotoFiles.DeleteByUrl(env, url);
            return Results.Redirect($"/dogs/{dogId}/edit");
        }).RequireAuthorization();

        // Medical records
        api.MapGet("/dogs/{id:int}/medical", async (int id, IDogService dogs) =>
            Results.Ok(await dogs.GetMedicalRecordsAsync(id)));

        api.MapPost("/dogs/{id:int}/medical", async (int id, CreateMedicalRecordRequest request, IDogService dogs) =>
        {
            var record = await dogs.AddMedicalRecordAsync(request with { DogId = id });
            return record is null
                ? Results.NotFound()
                : Results.Created($"/api/dogs/{id}/medical/{record.Id}", record);
        });

        api.MapGet("/dogs/{id:int}/medical/{recId:int}", async (int recId, IDogService dogs) =>
        {
            var record = await dogs.GetMedicalRecordAsync(recId);
            return record is null ? Results.NotFound() : Results.Ok(record);
        });

        api.MapGet("/medical/deleted", async (IDogService dogs) =>
            Results.Ok(await dogs.GetDeletedMedicalRecordsAsync())).RequireAuthorization("Manager");

        api.MapGet("/medical/deleted/{id:int}", async (int id, IDogService dogs) =>
        {
            var record = await dogs.GetDeletedMedicalRecordAsync(id);
            return record is null ? Results.NotFound() : Results.Ok(record);
        }).RequireAuthorization("Manager");

        api.MapGet("/medical/{id:int}", async (int id, IDogService dogs) =>
        {
            var record = await dogs.GetMedicalRecordAsync(id);
            return record is null ? Results.NotFound() : Results.Ok(record);
        });

        api.MapPut("/medical/{id:int}", async (int id, UpdateMedicalRecordRequest request, IDogService dogs) =>
        {
            var record = await dogs.UpdateMedicalRecordAsync(request with { Id = id });
            return record is null ? Results.NotFound() : Results.Ok(record);
        });

        api.MapDelete("/medical/{id:int}", async (int id, IDogService dogs) =>
            await dogs.DeleteMedicalRecordAsync(id) ? Results.NoContent() : Results.NotFound());

        api.MapPost("/medical/{id:int}/delete", async (int id, int? dogId, string? returnUrl, IDogService dogs) =>
        {
            await dogs.DeleteMedicalRecordAsync(id);
            return Results.Redirect(returnUrl ?? (dogId.HasValue ? $"/dogs/{dogId}" : "/dogs"));
        }).RequireAuthorization("Manager");

        api.MapPost("/medical/{id:int}/restore", async (int id, IDogService dogs) =>
        {
            await dogs.RestoreMedicalRecordAsync(id);
            return Results.Redirect("/admin/deleted?tab=medical");
        }).RequireAuthorization("Manager");

        api.MapPost("/medical/{id:int}/purge", async (int id, IDogService dogs) =>
        {
            await dogs.PurgeMedicalRecordAsync(id);
            return Results.Redirect("/admin/deleted?tab=medical");
        }).RequireAuthorization("Manager");

        // Medications
        api.MapGet("/dogs/{id:int}/medications", async (int id, IDogService dogs) =>
            Results.Ok(await dogs.GetMedicationsAsync(id)));

        api.MapPost("/dogs/{id:int}/medications", async (int id, CreateMedicationRequest request, IDogService dogs) =>
        {
            var medication = await dogs.AddMedicationAsync(request with { DogId = id });
            return medication is null
                ? Results.NotFound()
                : Results.Created($"/api/dogs/{id}/medications/{medication.Id}", medication);
        });

        api.MapGet("/medications/deleted", async (IDogService dogs) =>
            Results.Ok(await dogs.GetDeletedMedicationsAsync())).RequireAuthorization("Manager");

        api.MapGet("/medications/deleted/{id:int}", async (int id, IDogService dogs) =>
        {
            var medication = await dogs.GetDeletedMedicationAsync(id);
            return medication is null ? Results.NotFound() : Results.Ok(medication);
        }).RequireAuthorization("Manager");

        api.MapGet("/medications/{id:int}", async (int id, IDogService dogs) =>
        {
            var medication = await dogs.GetMedicationAsync(id);
            return medication is null ? Results.NotFound() : Results.Ok(medication);
        });

        api.MapPut("/medications/{id:int}", async (int id, UpdateMedicationRequest request, IDogService dogs) =>
        {
            var medication = await dogs.UpdateMedicationAsync(request with { Id = id });
            return medication is null ? Results.NotFound() : Results.Ok(medication);
        });

        api.MapPost("/medications/{id:int}/delete", async (int id, int? dogId, string? returnUrl, IDogService dogs) =>
        {
            await dogs.DeleteMedicationAsync(id);
            return Results.Redirect(returnUrl ?? (dogId.HasValue ? $"/dogs/{dogId}" : "/dogs"));
        }).RequireAuthorization("Manager");

        api.MapPost("/medications/{id:int}/restore", async (int id, IDogService dogs) =>
        {
            await dogs.RestoreMedicationAsync(id);
            return Results.Redirect("/admin/deleted?tab=medications");
        }).RequireAuthorization("Manager");

        api.MapPost("/medications/{id:int}/purge", async (int id, IDogService dogs) =>
        {
            await dogs.PurgeMedicationAsync(id);
            return Results.Redirect("/admin/deleted?tab=medications");
        }).RequireAuthorization("Manager");

        // Kept verb: external consumers "delete" a medication by deactivating the course.
        api.MapDelete("/medications/{id:int}", async (int id, IDogService dogs) =>
            await dogs.DeactivateMedicationAsync(id) ? Results.NoContent() : Results.NotFound());

        return api;
    }
}
