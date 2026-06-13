using Akka.Hosting;
using Refugio.Actors;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Queries;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;

namespace Refugio.Web.Endpoints;

public static class VolunteerEndpoints
{
    public static RouteGroupBuilder MapVolunteerEndpoints(this RouteGroupBuilder api)
    {
        // Volunteers
        api.MapGet("/volunteers", async (VolunteerStatus? status, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<VolunteerActor>().AskRequired<List<VolunteerDto>>(new GetVolunteers(status), ct)));

        api.MapGet("/volunteers/paged", async (VolunteerStatus? status, int? page, int? pageSize, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<VolunteerActor>().AskRequired<Page<VolunteerDto>>(new GetVolunteersPaged(status, page ?? 1, pageSize ?? 25), ct)));

        api.MapGet("/volunteers/counts", async (IVolunteerQueries volunteerQueries) =>
            Results.Ok(await volunteerQueries.GetCountsAsync()));

        api.MapGet("/volunteers/deleted", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<VolunteerActor>().AskRequired<List<VolunteerDto>>(new GetDeletedVolunteers(), ct)))
            .RequireAuthorization("Manager");

        api.MapGet("/volunteers/deleted/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var volunteer = await actors.Get<VolunteerActor>().AskFor<VolunteerDto>(new GetDeletedVolunteer(id), ct);
            return volunteer is null ? Results.NotFound() : Results.Ok(volunteer);
        }).RequireAuthorization("Manager");

        api.MapGet("/volunteers/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var volunteer = await actors.Get<VolunteerActor>().AskFor<VolunteerDto>(new GetVolunteer(id), ct);
            return volunteer is null ? Results.NotFound() : Results.Ok(volunteer);
        });

        api.MapPut("/volunteers/{id:int}", async (int id, UpdateVolunteerRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var volunteer = await actors.Get<VolunteerActor>().AskFor<VolunteerDto>(request with { Id = id }, ct);
            return volunteer is null ? Results.NotFound() : Results.Ok(volunteer);
        });

        api.MapPost("/volunteers", async (CreateVolunteerRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var volunteer = await actors.Get<VolunteerActor>().AskRequired<VolunteerDto>(request, ct);
            return Results.Created($"/api/volunteers/{volunteer.Id}", volunteer);
        });

        api.MapPut("/volunteers/{id:int}/status", async (int id, UpdateVolunteerStatusRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var volunteer = await actors.Get<VolunteerActor>().AskFor<VolunteerDto>(new ChangeVolunteerStatus(id, request.Status), ct);
            return volunteer is null ? Results.NotFound() : Results.Ok(volunteer);
        });

        api.MapPost("/volunteers/{id:int}/activate", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<VolunteerActor>().AskFor<VolunteerDto>(new ChangeVolunteerStatus(id, VolunteerStatus.Active), ct);
            return Results.Redirect("/volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/deactivate", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<VolunteerActor>().AskFor<VolunteerDto>(new ChangeVolunteerStatus(id, VolunteerStatus.Inactive), ct);
            return Results.Redirect("/volunteers");
        }).RequireAuthorization("Manager");

        api.MapDelete("/volunteers/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<VolunteerActor>().AskRequired<bool>(new DeleteVolunteer(id), ct)
                ? Results.NoContent() : Results.NotFound());

        api.MapPost("/volunteers/{id:int}/delete", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<VolunteerActor>().AskRequired<bool>(new DeleteVolunteer(id), ct);
            return Results.Redirect("/volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/restore", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<VolunteerActor>().AskRequired<bool>(new RestoreVolunteer(id), ct);
            return Results.Redirect("/admin/deleted?tab=volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/purge", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<VolunteerActor>().AskRequired<bool>(new PurgeVolunteer(id), ct);
            return Results.Redirect("/admin/deleted?tab=volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/photo", async (int id, HttpContext ctx, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var file = ctx.Request.Form.Files.GetFile("Photo");
            if (file is null || file.Length == 0) return Results.Redirect($"/volunteers/{id}");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".png")) return Results.Redirect($"/volunteers/{id}");
            if (file.Length > 2 * 1024 * 1024) return Results.Redirect($"/volunteers/{id}");
            var dir = Path.Combine(env.WebRootPath, "photos", "volunteer", id.ToString());
            Directory.CreateDirectory(dir);
            foreach (var old in Directory.GetFiles(dir, "primary.*")) File.Delete(old);
            var fileName = $"primary{ext}";
            await using var stream = File.Create(Path.Combine(dir, fileName));
            await file.CopyToAsync(stream);
            await actors.Get<VolunteerActor>().AskRequired<bool>(new SetVolunteerPhoto(id, $"/photos/volunteer/{id}/{fileName}"), ct);
            return Results.Redirect($"/volunteers/{id}");
        }).RequireAuthorization().DisableAntiforgery();

        api.MapPost("/volunteers/{id:int}/photo/delete", async (int id, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            await actors.Get<VolunteerActor>().AskRequired<bool>(new SetVolunteerPhoto(id, null), ct);
            var dir = Path.Combine(env.WebRootPath, "photos", "volunteer", id.ToString());
            if (Directory.Exists(dir))
                foreach (var f in Directory.GetFiles(dir, "primary.*")) File.Delete(f);
            return Results.Redirect($"/volunteers/{id}");
        }).RequireAuthorization();

        // Events / Calendar
        api.MapGet("/events", async (DateTime? from, DateTime? to, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<EventActor>().AskRequired<List<ShelterEventDto>>(new GetEvents(from, to), ct)));

        api.MapGet("/events/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var shelterEvent = await actors.Get<EventActor>().AskFor<ShelterEventDto>(new GetEvent(id), ct);
            return shelterEvent is null ? Results.NotFound() : Results.Ok(shelterEvent);
        });

        api.MapPost("/events", async (CreateEventRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var shelterEvent = await actors.Get<EventActor>().AskRequired<ShelterEventDto>(request, ct);
            return Results.Created($"/api/events/{shelterEvent.Id}", shelterEvent);
        });

        api.MapPut("/events/{id:int}", async (int id, UpdateEventRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var shelterEvent = await actors.Get<EventActor>().AskFor<ShelterEventDto>(request with { Id = id }, ct);
            return shelterEvent is null ? Results.NotFound() : Results.Ok(shelterEvent);
        });

        api.MapDelete("/events/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<EventActor>().AskRequired<bool>(new DeleteEvent(id), ct)
                ? Results.NoContent() : Results.NotFound());

        api.MapPost("/events/{id:int}/delete", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<EventActor>().AskRequired<bool>(new DeleteEvent(id), ct);
            return Results.Redirect("/calendar");
        }).RequireAuthorization("Manager");

        return api;
    }
}
