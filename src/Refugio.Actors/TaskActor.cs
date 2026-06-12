using Microsoft.Extensions.DependencyInjection;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Services;

namespace Refugio.Actors;

/// <summary>Routes shelter-task operations to ITaskService.</summary>
public sealed class TaskActor : ShelterActorBase<ITaskService>
{
    public TaskActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        Query<GetTasks>(async (s, m) => await s.GetTasksAsync(m.IncludeCompleted));
        Command<CreateTaskRequest>(async (s, m) => await s.CreateAsync(m));
        Command<CompleteTask>(async (s, m) => await s.CompleteAsync(m.Id));
        Command<DeleteTask>(async (s, m) => await s.DeleteAsync(m.Id));
    }
}
