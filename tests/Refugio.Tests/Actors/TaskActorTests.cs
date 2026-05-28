using Akka.Actor;
using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Actors;

public class TaskActorTests : ActorTestBase
{
    private readonly IActorRef _actor;

    public TaskActorTests()
    {
        _actor = Sys.ActorOf(Props.Create(() => new TaskActor(_sf)));
    }

    private async Task<ShelterTask> SeedTask(bool completed = false)
        => await SeedAsync(db =>
        {
            var t = new ShelterTask
            {
                Title = "Feed dogs", DueDateTime = DateTime.UtcNow.AddHours(2), IsCompleted = completed
            };
            db.Tasks.Add(t);
            return t;
        });

    [Fact]
    public async Task GetAllTasks_ReturnsOnlyIncomplete_ByDefault()
    {
        await SeedTask(completed: false);
        await SeedTask(completed: true);
        var result = await _actor.Ask<List<ShelterTask>>(new GetAllTasks(IncludeCompleted: false), TimeSpan.FromSeconds(5));
        Assert.Single(result);
        Assert.False(result[0].IsCompleted);
    }

    [Fact]
    public async Task GetAllTasks_IncludesCompleted_WhenFlagTrue()
    {
        await SeedTask(completed: false);
        await SeedTask(completed: true);
        var result = await _actor.Ask<List<ShelterTask>>(new GetAllTasks(IncludeCompleted: true), TimeSpan.FromSeconds(5));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllTasks_ReturnsEmpty_WhenNone()
    {
        var result = await _actor.Ask<List<ShelterTask>>(new GetAllTasks(), TimeSpan.FromSeconds(5));
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateTask_CreatesAndReturns()
    {
        var volunteer = await SeedAsync(db =>
        {
            var v = new Volunteer { Name = "Alice", Email = "alice@test.com", Role = "Walker" };
            db.Volunteers.Add(v);
            return v;
        });
        var due = DateTime.UtcNow.AddDays(1);
        var result = await _actor.Ask<ShelterTask>(
            new CreateTask("Walk dogs", due, "daily walk", volunteer.Id, "North Yard"),
            TimeSpan.FromSeconds(5));
        Assert.Equal("Walk dogs", result.Title);
        Assert.Equal("Alice", result.AssignedTo);
        Assert.Equal("North Yard", result.Location);
        Assert.False(result.IsCompleted);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateTask_NullableFieldsAreNull_WhenNotProvided()
    {
        var due = DateTime.UtcNow.AddDays(1);
        var result = await _actor.Ask<ShelterTask>(
            new CreateTask("Clean kennel", due, null, (int?)null, null),
            TimeSpan.FromSeconds(5));
        Assert.Null(result.Notes);
        Assert.Null(result.AssignedTo);
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task CompleteTask_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedTask(completed: false);
        var result = await _actor.Ask<bool>(new CompleteTask(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task CompleteTask_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new CompleteTask(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    [Fact]
    public async Task CompleteTask_MarksTaskAsCompleted()
    {
        var seeded = await SeedTask(completed: false);
        await _actor.Ask<bool>(new CompleteTask(seeded.Id), TimeSpan.FromSeconds(5));

        // Verify via GetAllTasks — completed task disappears from default list
        var remaining = await _actor.Ask<List<ShelterTask>>(new GetAllTasks(IncludeCompleted: false), TimeSpan.FromSeconds(5));
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DeleteTask_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedTask();
        var result = await _actor.Ask<bool>(new DeleteTask(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteTask_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new DeleteTask(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }
}
