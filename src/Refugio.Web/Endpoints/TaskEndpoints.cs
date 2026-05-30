using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;

namespace Refugio.Web.Endpoints;

public static class TaskEndpoints
{
    public static RouteGroupBuilder MapTaskEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/tasks", async (bool? includeCompleted, ShelterActorService actors) =>
            Results.Ok(await actors.Ask<List<ShelterTask>>(new GetAllTasks(includeCompleted))));

        api.MapPost("/tasks", async (CreateTask cmd, ShelterActorService actors) =>
        {
            var t = await actors.Ask<ShelterTask>(cmd);
            return Results.Created($"/api/tasks/{t.Id}", t);
        });

        api.MapPut("/tasks/{id:int}/complete", async (int id, ShelterActorService actors) =>
        {
            var ok = await actors.Ask<bool>(new CompleteTask(id));
            return ok ? Results.NoContent() : Results.NotFound();
        });

        api.MapPost("/tasks/{id:int}/complete", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new CompleteTask(id));
            return Results.Redirect("/");
        }).RequireAuthorization();

        api.MapPost("/tasks/{id:int}/delete", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new DeleteTask(id));
            return Results.Redirect("/");
        }).RequireAuthorization("Manager");

        api.MapDelete("/tasks/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var ok = await actors.Ask<bool>(new DeleteTask(id));
            return ok ? Results.NoContent() : Results.NotFound();
        });

        return api;
    }
}
