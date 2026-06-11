using Refugio.Application.Contracts;
using Refugio.Application.Queries;
using Refugio.Application.Services;
using Refugio.Domain.Entities;

namespace Refugio.Web.Endpoints;

public static class VolunteerEndpoints
{
    public static RouteGroupBuilder MapVolunteerEndpoints(this RouteGroupBuilder api)
    {
        // Volunteers
        api.MapGet("/volunteers", async (VolunteerStatus? status, IVolunteerService volunteers) =>
            Results.Ok(await volunteers.GetVolunteersAsync(status)));

        api.MapGet("/volunteers/paged", async (VolunteerStatus? status, int? page, int? pageSize, IVolunteerService volunteers) =>
            Results.Ok(await volunteers.GetVolunteersPagedAsync(status, page ?? 1, pageSize ?? 25)));

        api.MapGet("/volunteers/counts", async (IVolunteerQueries volunteerQueries) =>
            Results.Ok(await volunteerQueries.GetCountsAsync()));

        api.MapGet("/volunteers/deleted", async (IVolunteerService volunteers) =>
            Results.Ok(await volunteers.GetDeletedAsync())).RequireAuthorization("Manager");

        api.MapGet("/volunteers/deleted/{id:int}", async (int id, IVolunteerService volunteers) =>
        {
            var volunteer = await volunteers.GetDeletedByIdAsync(id);
            return volunteer is null ? Results.NotFound() : Results.Ok(volunteer);
        }).RequireAuthorization("Manager");

        api.MapGet("/volunteers/{id:int}", async (int id, IVolunteerService volunteers) =>
        {
            var volunteer = await volunteers.GetVolunteerAsync(id);
            return volunteer is null ? Results.NotFound() : Results.Ok(volunteer);
        });

        api.MapPut("/volunteers/{id:int}", async (int id, UpdateVolunteerRequest request, IVolunteerService volunteers) =>
        {
            var volunteer = await volunteers.UpdateAsync(request with { Id = id });
            return volunteer is null ? Results.NotFound() : Results.Ok(volunteer);
        });

        api.MapPost("/volunteers", async (CreateVolunteerRequest request, IVolunteerService volunteers) =>
        {
            var volunteer = await volunteers.RegisterAsync(request);
            return Results.Created($"/api/volunteers/{volunteer.Id}", volunteer);
        });

        api.MapPut("/volunteers/{id:int}/status", async (int id, UpdateVolunteerStatusRequest request, IVolunteerService volunteers) =>
        {
            var volunteer = await volunteers.ChangeStatusAsync(id, request.Status);
            return volunteer is null ? Results.NotFound() : Results.Ok(volunteer);
        });

        api.MapPost("/volunteers/{id:int}/activate", async (int id, IVolunteerService volunteers) =>
        {
            await volunteers.ChangeStatusAsync(id, VolunteerStatus.Active);
            return Results.Redirect("/volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/deactivate", async (int id, IVolunteerService volunteers) =>
        {
            await volunteers.ChangeStatusAsync(id, VolunteerStatus.Inactive);
            return Results.Redirect("/volunteers");
        }).RequireAuthorization("Manager");

        api.MapDelete("/volunteers/{id:int}", async (int id, IVolunteerService volunteers) =>
            await volunteers.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

        api.MapPost("/volunteers/{id:int}/delete", async (int id, IVolunteerService volunteers) =>
        {
            await volunteers.DeleteAsync(id);
            return Results.Redirect("/volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/restore", async (int id, IVolunteerService volunteers) =>
        {
            await volunteers.RestoreAsync(id);
            return Results.Redirect("/admin/deleted?tab=volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/purge", async (int id, IVolunteerService volunteers) =>
        {
            await volunteers.PurgeAsync(id);
            return Results.Redirect("/admin/deleted?tab=volunteers");
        }).RequireAuthorization("Manager");

        api.MapPost("/volunteers/{id:int}/photo", async (int id, HttpContext ctx, IVolunteerService volunteers, IWebHostEnvironment env) =>
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
            await volunteers.SetPhotoAsync(id, $"/volunteers/{fileName}");
            return Results.Redirect($"/volunteers/{id}");
        }).RequireAuthorization().DisableAntiforgery();

        api.MapPost("/volunteers/{id:int}/photo/delete", async (int id, IVolunteerService volunteers, IWebHostEnvironment env) =>
        {
            await volunteers.SetPhotoAsync(id, null);
            var dir = Path.Combine(env.WebRootPath, "volunteers");
            if (Directory.Exists(dir))
                foreach (var f in Directory.GetFiles(dir, $"{id}.*")) File.Delete(f);
            return Results.Redirect($"/volunteers/{id}");
        }).RequireAuthorization();

        // Events / Calendar
        api.MapGet("/events", async (DateTime? from, DateTime? to, IEventService events) =>
            Results.Ok(await events.GetEventsAsync(from, to)));

        api.MapGet("/events/{id:int}", async (int id, IEventService events) =>
        {
            var shelterEvent = await events.GetEventAsync(id);
            return shelterEvent is null ? Results.NotFound() : Results.Ok(shelterEvent);
        });

        api.MapPost("/events", async (CreateEventRequest request, IEventService events) =>
        {
            var shelterEvent = await events.ScheduleAsync(request);
            return Results.Created($"/api/events/{shelterEvent.Id}", shelterEvent);
        });

        api.MapPut("/events/{id:int}", async (int id, UpdateEventRequest request, IEventService events) =>
        {
            var shelterEvent = await events.UpdateAsync(request with { Id = id });
            return shelterEvent is null ? Results.NotFound() : Results.Ok(shelterEvent);
        });

        api.MapDelete("/events/{id:int}", async (int id, IEventService events) =>
            await events.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

        api.MapPost("/events/{id:int}/delete", async (int id, IEventService events) =>
        {
            await events.DeleteAsync(id);
            return Results.Redirect("/calendar");
        }).RequireAuthorization("Manager");

        return api;
    }
}
