using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Infrastructure.Data;

namespace Refugio.Application.Actors;

public class TaskActor : ReceiveActor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TaskActor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        ReceiveAsync<GetAllTasks>(Handle);
        ReceiveAsync<CreateTask>(Handle);
        ReceiveAsync<CompleteTask>(Handle);
        ReceiveAsync<DeleteTask>(Handle);
    }

    private ShelterDbContext Db(IServiceScope s) => s.ServiceProvider.GetRequiredService<ShelterDbContext>();

    private async Task Handle(GetAllTasks msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var q = Db(scope).Tasks.Include(t => t.AssignedVolunteer).AsQueryable();
        if (msg.IncludeCompleted != true) q = q.Where(t => !t.IsCompleted);
        Sender.Tell(await q.OrderBy(t => t.DueDateTime).ToListAsync());
    }

    private async Task Handle(CreateTask msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
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
    }

    private async Task Handle(CompleteTask msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var task = await db.Tasks.FindAsync(msg.Id);
        if (task is null) { Sender.Tell(false); return; }
        task.IsCompleted = true;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(DeleteTask msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var task = await db.Tasks.FindAsync(msg.Id);
        if (task is null) { Sender.Tell(false); return; }
        task.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }
}
