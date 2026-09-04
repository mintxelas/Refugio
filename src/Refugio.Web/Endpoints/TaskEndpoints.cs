using Akka.Hosting;
using Refugio.Actors;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;

namespace Refugio.Web.Endpoints;

public static class TaskEndpoints
{
    public static RouteGroupBuilder MapTaskEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/tasks", async (bool? includeCompleted, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<TaskActor>().AskRequired<List<ShelterTaskDto>>(new GetTasks(includeCompleted ?? false), ct)));

        api.MapPost("/tasks", async (CreateTaskRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var task = await actors.Get<TaskActor>().AskRequired<ShelterTaskDto>(request, ct);
            return Results.Created($"/api/tasks/{task.Id}", task);
        });

        api.MapPut("/tasks/{id:int}/complete", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<TaskActor>().AskRequired<bool>(new CompleteTask(id), ct) ? Results.NoContent() : Results.NotFound());

        api.MapPost("/tasks/{id:int}/complete", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<TaskActor>().AskRequired<bool>(new CompleteTask(id), ct);
            return Results.Redirect("/");
        }).RequireAuthorization();

        api.MapPost("/tasks/{id:int}/delete", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<TaskActor>().AskRequired<bool>(new DeleteTask(id), ct);
            return Results.Redirect("/");
        }).RequireAuthorization("Manager");

        api.MapDelete("/tasks/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<TaskActor>().AskRequired<bool>(new DeleteTask(id), ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("Manager");

        return api;
    }
}
