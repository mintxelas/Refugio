using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;

namespace Refugio.Web.Endpoints;

public static class VolunteerEndpoints
{
    public static RouteGroupBuilder MapVolunteerEndpoints(this RouteGroupBuilder api)
    {
        // Volunteers
        api.MapGet("/volunteers", async (VolunteerStatus? status, ShelterActorService actors) =>
            Results.Ok(await actors.Ask<List<Volunteer>>(new GetAllVolunteers(status))));

        api.MapGet("/volunteers/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var v = await actors.Ask<Volunteer?>(new GetVolunteerById(id));
            return v is null ? Results.NotFound() : Results.Ok(v);
        });

        api.MapPut("/volunteers/{id:int}", async (int id, UpdateVolunteer cmd, ShelterActorService actors) =>
        {
            var v = await actors.Ask<Volunteer?>(cmd with { Id = id });
            return v is null ? Results.NotFound() : Results.Ok(v);
        });

        api.MapPost("/volunteers", async (CreateVolunteer cmd, ShelterActorService actors) =>
        {
            var v = await actors.Ask<Volunteer>(cmd);
            return Results.Created($"/api/volunteers/{v.Id}", v);
        });

        api.MapPut("/volunteers/{id:int}/status", async (int id, UpdateVolunteerStatus cmd, ShelterActorService actors) =>
        {
            var v = await actors.Ask<Volunteer?>(cmd with { Id = id });
            return v is null ? Results.NotFound() : Results.Ok(v);
        });

        api.MapPost("/volunteers/{id:int}/activate", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<Volunteer?>(new UpdateVolunteerStatus(id, VolunteerStatus.Active));
            return Results.Redirect("/volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/deactivate", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<Volunteer?>(new UpdateVolunteerStatus(id, VolunteerStatus.Inactive));
            return Results.Redirect("/volunteers");
        }).RequireAuthorization("Manager");

        api.MapDelete("/volunteers/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var ok = await actors.Ask<bool>(new DeleteVolunteer(id));
            return ok ? Results.NoContent() : Results.NotFound();
        });

        api.MapPost("/volunteers/{id:int}/delete", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new DeleteVolunteer(id));
            return Results.Redirect("/volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/restore", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new RestoreVolunteer(id));
            return Results.Redirect("/admin/deleted?tab=volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/purge", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new PermanentDeleteVolunteer(id));
            return Results.Redirect("/admin/deleted?tab=volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/photo", async (int id, HttpContext ctx, ShelterActorService actors, IWebHostEnvironment env) =>
        {
            var file = ctx.Request.Form.Files.GetFile("Photo");
            if (file is null || file.Length == 0) return Results.Redirect($"/volunteers/{id}");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) return Results.Redirect($"/volunteers/{id}");
            if (file.Length > 5 * 1024 * 1024) return Results.Redirect($"/volunteers/{id}");
            var dir = Path.Combine(env.WebRootPath, "volunteers");
            Directory.CreateDirectory(dir);
            foreach (var old in Directory.GetFiles(dir, $"{id}.*")) File.Delete(old);
            var fileName = $"{id}{ext}";
            await using var stream = File.Create(Path.Combine(dir, fileName));
            await file.CopyToAsync(stream);
            await actors.Ask<bool>(new UpdateVolunteerPhoto(id, $"/volunteers/{fileName}"));
            return Results.Redirect($"/volunteers/{id}");
        }).RequireAuthorization().DisableAntiforgery();

        api.MapPost("/volunteers/{id:int}/photo/delete", async (int id, ShelterActorService actors, IWebHostEnvironment env) =>
        {
            await actors.Ask<bool>(new UpdateVolunteerPhoto(id, null));
            var dir = Path.Combine(env.WebRootPath, "volunteers");
            if (Directory.Exists(dir))
                foreach (var f in Directory.GetFiles(dir, $"{id}.*")) File.Delete(f);
            return Results.Redirect($"/volunteers/{id}");
        }).RequireAuthorization();

        // Events / Calendar
        api.MapGet("/events", async (DateTime? from, DateTime? to, ShelterActorService actors) =>
            Results.Ok(await actors.Ask<List<ShelterEvent>>(new GetAllEvents(from, to))));

        api.MapGet("/events/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var e = await actors.Ask<ShelterEvent?>(new GetEventById(id));
            return e is null ? Results.NotFound() : Results.Ok(e);
        });

        api.MapPost("/events", async (CreateEvent cmd, ShelterActorService actors) =>
        {
            var e = await actors.Ask<ShelterEvent>(cmd);
            return Results.Created($"/api/events/{e.Id}", e);
        });

        api.MapPut("/events/{id:int}", async (int id, UpdateEvent cmd, ShelterActorService actors) =>
        {
            var e = await actors.Ask<ShelterEvent?>(cmd with { Id = id });
            return e is null ? Results.NotFound() : Results.Ok(e);
        });

        api.MapDelete("/events/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var ok = await actors.Ask<bool>(new DeleteEvent(id));
            return ok ? Results.NoContent() : Results.NotFound();
        });

        api.MapPost("/events/{id:int}/delete", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new DeleteEvent(id));
            return Results.Redirect("/calendar");
        }).RequireAuthorization("Manager");

        return api;
    }
}
