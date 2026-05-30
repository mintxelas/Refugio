using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;

namespace Refugio.Application.Actors;

public class TaskActor : ShelterActorBase
{
    public TaskActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        ReceiveAsync<GetAllTasks>(Handle);
        ReceiveAsync<CreateTask>(Handle);
        ReceiveAsync<CompleteTask>(Handle);
        ReceiveAsync<DeleteTask>(msg => SoftDelete<ShelterTask>(msg.Id));
    }

    private Task Handle(GetAllTasks msg) => WithDb(async db =>
    {
        var q = db.Tasks.Include(t => t.AssignedVolunteer).AsQueryable();
        if (msg.IncludeCompleted != true) q = q.Where(t => !t.IsCompleted);
        Sender.Tell(await q.OrderBy(t => t.DueDateTime).ToListAsync());
    });

    private Task Handle(CreateTask msg) => WithDb(async db =>
    {
        var task = new ShelterTask
        {
            Title = msg.Title,
            DueDateTime = msg.DueDateTime,
            Notes = msg.Notes,
            Location = msg.Location,
            AssignedVolunteerId = msg.AssignedVolunteerId,
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        Sender.Tell(task);
    });

    private Task Handle(CompleteTask msg) => WithDb(async db =>
    {
        var task = await db.Tasks.FindAsync(msg.Id);
        if (task is null) { Sender.Tell(false); return; }
        task.IsCompleted = true;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    });
}
