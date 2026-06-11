using Refugio.Application.Contracts;
using Refugio.Application.Services;

namespace Refugio.Web.Endpoints;

public static class TaskEndpoints
{
    public static RouteGroupBuilder MapTaskEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/tasks", async (bool? includeCompleted, ITaskService tasks) =>
            Results.Ok(await tasks.GetTasksAsync(includeCompleted ?? false)));

        api.MapPost("/tasks", async (CreateTaskRequest request, ITaskService tasks) =>
        {
            var task = await tasks.CreateAsync(request);
            return Results.Created($"/api/tasks/{task.Id}", task);
        });

        api.MapPut("/tasks/{id:int}/complete", async (int id, ITaskService tasks) =>
            await tasks.CompleteAsync(id) ? Results.NoContent() : Results.NotFound());

        api.MapPost("/tasks/{id:int}/complete", async (int id, ITaskService tasks) =>
        {
            await tasks.CompleteAsync(id);
            return Results.Redirect("/");
        }).RequireAuthorization();

        api.MapPost("/tasks/{id:int}/delete", async (int id, ITaskService tasks) =>
        {
            await tasks.DeleteAsync(id);
            return Results.Redirect("/");
        }).RequireAuthorization("Manager");

        api.MapDelete("/tasks/{id:int}", async (int id, ITaskService tasks) =>
            await tasks.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

        return api;
    }
}
