using Refugio.Application.Contracts;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Services;

public class TaskServiceTests : ServiceTestBase
{
    private Task<T> Svc<T>(Func<ITaskService, Task<T>> action) => WithServiceAsync(action);

    private Task<ShelterTask> SeedTask(bool completed = false)
        => SeedAsync(db =>
        {
            var task = ShelterTask.Create("Feed dogs", DateTime.UtcNow.AddHours(2));
            if (completed) task.Complete();
            db.Tasks.Add(task);
            return task;
        });

    [Fact]
    public async Task GetTasks_ReturnsOnlyIncomplete_ByDefault()
    {
        await SeedTask(completed: false);
        await SeedTask(completed: true);
        var result = await Svc(s => s.GetTasksAsync(includeCompleted: false));
        Assert.Single(result);
        Assert.False(result[0].IsCompleted);
    }

    [Fact]
    public async Task GetTasks_IncludesCompleted_WhenFlagTrue()
    {
        await SeedTask(completed: false);
        await SeedTask(completed: true);
        Assert.Equal(2, (await Svc(s => s.GetTasksAsync(includeCompleted: true))).Count);
    }

    [Fact]
    public async Task GetTasks_ReturnsEmpty_WhenNone()
    {
        Assert.Empty(await Svc(s => s.GetTasksAsync()));
    }

    [Fact]
    public async Task GetTasks_IncludesAssignedVolunteer()
    {
        var volunteer = await SeedAsync(db =>
        {
            var v = Volunteer.Register("Alice", "alice@test.com", null, Refugio.Domain.Helpers.Roles.Volunteer, null);
            db.Volunteers.Add(v);
            return v;
        });
        await SeedAsync(db =>
        {
            var task = ShelterTask.Create("Walk", DateTime.UtcNow, assignedVolunteerId: volunteer.Id);
            db.Tasks.Add(task);
            return task;
        });
        var result = await Svc(s => s.GetTasksAsync());
        Assert.Single(result);
        Assert.NotNull(result[0].AssignedVolunteer);
        Assert.Equal("Alice", result[0].AssignedVolunteer!.Name);
    }

    [Fact]
    public async Task Create_CreatesAndReturns()
    {
        var volunteer = await SeedAsync(db =>
        {
            var v = Volunteer.Register("Alice", "alice@test.com", null, Refugio.Domain.Helpers.Roles.Volunteer, null);
            db.Volunteers.Add(v);
            return v;
        });
        var due = DateTime.UtcNow.AddDays(1);
        var result = await Svc(s => s.CreateAsync(
            new CreateTaskRequest("Walk dogs", due, "daily walk", volunteer.Id, "North Yard")));
        Assert.Equal("Walk dogs", result.Title);
        Assert.Equal(volunteer.Id, result.AssignedVolunteerId);
        Assert.Equal("North Yard", result.Location);
        Assert.False(result.IsCompleted);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task Create_NullableFieldsAreNull_WhenNotProvided()
    {
        var result = await Svc(s => s.CreateAsync(
            new CreateTaskRequest("Clean kennel", DateTime.UtcNow.AddDays(1), null, null, null)));
        Assert.Null(result.Notes);
        Assert.Null(result.AssignedVolunteerId);
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task Complete_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedTask();
        Assert.True(await Svc(s => s.CompleteAsync(seeded.Id)));
    }

    [Fact]
    public async Task Complete_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.CompleteAsync(99999)));
    }

    [Fact]
    public async Task Complete_MarksTaskAsCompleted()
    {
        var seeded = await SeedTask();
        await Svc(s => s.CompleteAsync(seeded.Id));
        Assert.Empty(await Svc(s => s.GetTasksAsync(includeCompleted: false)));
    }

    [Fact]
    public async Task Delete_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedTask();
        Assert.True(await Svc(s => s.DeleteAsync(seeded.Id)));
    }

    [Fact]
    public async Task Delete_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.DeleteAsync(99999)));
    }
}
