using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;

namespace Refugio.Web.Endpoints;

public static class DogEndpoints
{
    public static RouteGroupBuilder MapDogEndpoints(this RouteGroupBuilder api)
    {
        // Dashboard
        api.MapGet("/dashboard", async (ShelterActorService actors) =>
            Results.Ok(await actors.Ask<DashboardStats>(new GetDashboardStats())));

        // Dogs
        api.MapGet("/dogs", async (string? search, DogStatus? status, ShelterActorService actors) =>
            Results.Ok(await actors.Ask<List<Dog>>(new GetAllDogs(search, status))));

        api.MapGet("/dogs/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var dog = await actors.Ask<Dog?>(new GetDogById(id));
            return dog is null ? Results.NotFound() : Results.Ok(dog);
        });

        api.MapPost("/dogs", async (CreateDog cmd, ShelterActorService actors) =>
        {
            var dog = await actors.Ask<Dog>(cmd);
            return Results.Created($"/api/dogs/{dog.Id}", dog);
        });

        api.MapPut("/dogs/{id:int}", async (int id, UpdateDog cmd, ShelterActorService actors) =>
        {
            var dog = await actors.Ask<Dog?>(cmd with { Id = id });
            return dog is null ? Results.NotFound() : Results.Ok(dog);
        });

        api.MapDelete("/dogs/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var ok = await actors.Ask<bool>(new DeleteDog(id));
            return ok ? Results.NoContent() : Results.NotFound();
        });

        api.MapPost("/dogs/{id:int}/delete", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new DeleteDog(id));
            return Results.Redirect("/dogs");
        }).RequireAuthorization("Manager");

        api.MapPost("/dogs/{id:int}/restore", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new RestoreDog(id));
            return Results.Redirect("/admin/deleted?tab=dogs");
        }).RequireAuthorization("Manager");

        api.MapPost("/dogs/{id:int}/purge", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new PermanentDeleteDog(id));
            return Results.Redirect("/admin/deleted?tab=dogs");
        }).RequireAuthorization("Manager");

        api.MapPost("/dogs/{id:int}/photo", async (int id, HttpContext ctx, ShelterActorService actors, IWebHostEnvironment env) =>
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
            await actors.Ask<bool>(new UpdateDogPhoto(id, $"/dogs/{fileName}"));
            return Results.Redirect($"/dogs/{id}/edit");
        }).RequireAuthorization().DisableAntiforgery();

        // Medical records
        api.MapGet("/dogs/{id:int}/medical", async (int id, ShelterActorService actors) =>
            Results.Ok(await actors.Ask<List<MedicalRecord>>(new GetMedicalRecords(id))));

        api.MapPost("/dogs/{id:int}/medical", async (int id, CreateMedicalRecord cmd, ShelterActorService actors) =>
        {
            var rec = await actors.Ask<MedicalRecord>(cmd with { DogId = id });
            return Results.Created($"/api/dogs/{id}/medical/{rec.Id}", rec);
        });

        api.MapGet("/dogs/{id:int}/medical/{recId:int}", async (int recId, ShelterActorService actors) =>
        {
            var rec = await actors.Ask<MedicalRecord?>(new GetMedicalRecordById(recId));
            return rec is null ? Results.NotFound() : Results.Ok(rec);
        });

        api.MapPost("/medical/{id:int}/delete", async (int id, int? dogId, string? returnUrl, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new DeleteMedicalRecord(id));
            return Results.Redirect(returnUrl ?? (dogId.HasValue ? $"/dogs/{dogId}" : "/dogs"));
        }).RequireAuthorization("Manager");

        api.MapPost("/medical/{id:int}/restore", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new RestoreMedicalRecord(id));
            return Results.Redirect("/admin/deleted?tab=medical");
        }).RequireAuthorization("Manager");

        api.MapPost("/medical/{id:int}/purge", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new PermanentDeleteMedicalRecord(id));
            return Results.Redirect("/admin/deleted?tab=medical");
        }).RequireAuthorization("Manager");

        // Medications
        api.MapGet("/dogs/{id:int}/medications", async (int id, ShelterActorService actors) =>
            Results.Ok(await actors.Ask<List<Medication>>(new GetMedications(id))));

        api.MapPost("/dogs/{id:int}/medications", async (int id, CreateMedication cmd, ShelterActorService actors) =>
        {
            var med = await actors.Ask<Medication>(cmd with { DogId = id });
            return Results.Created($"/api/dogs/{id}/medications/{med.Id}", med);
        });

        api.MapGet("/medications/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var med = await actors.Ask<Medication?>(new GetMedicationById(id));
            return med is null ? Results.NotFound() : Results.Ok(med);
        });

        api.MapPost("/medications/{id:int}/delete", async (int id, int? dogId, string? returnUrl, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new DeleteMedication(id));
            return Results.Redirect(returnUrl ?? (dogId.HasValue ? $"/dogs/{dogId}" : "/dogs"));
        }).RequireAuthorization("Manager");

        api.MapPost("/medications/{id:int}/restore", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new RestoreMedication(id));
            return Results.Redirect("/admin/deleted?tab=medications");
        }).RequireAuthorization("Manager");

        api.MapPost("/medications/{id:int}/purge", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new PermanentDeleteMedication(id));
            return Results.Redirect("/admin/deleted?tab=medications");
        }).RequireAuthorization("Manager");

        api.MapDelete("/medications/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var ok = await actors.Ask<bool>(new DeactivateMedication(id));
            return ok ? Results.NoContent() : Results.NotFound();
        });

        return api;
    }
}
