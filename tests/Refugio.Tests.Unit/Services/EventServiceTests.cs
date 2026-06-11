using Refugio.Application.Contracts;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Services;

public class EventServiceTests : ServiceTestBase
{
    private Task<T> Svc<T>(Func<IEventService, Task<T>> action) => WithServiceAsync(action);

    private Task<ShelterEvent> SeedEvent(DateTime? start = null)
        => SeedAsync(db =>
        {
            var startAt = start ?? DateTime.UtcNow;
            var shelterEvent = ShelterEvent.Schedule("Adoption Fair", startAt, startAt.AddHours(4), eventType: "Adoption");
            db.Events.Add(shelterEvent);
            return shelterEvent;
        });

    [Fact]
    public async Task GetEvents_ReturnsEmpty_WhenNone()
    {
        Assert.Empty(await Svc(s => s.GetEventsAsync()));
    }

    [Fact]
    public async Task GetEvents_ReturnsAll()
    {
        await SeedEvent(new DateTime(2025, 3, 1));
        await SeedEvent(new DateTime(2025, 6, 1));
        Assert.Equal(2, (await Svc(s => s.GetEventsAsync())).Count);
    }

    [Fact]
    public async Task GetEvents_FiltersByFrom()
    {
        await SeedEvent(new DateTime(2025, 1, 1));
        await SeedEvent(new DateTime(2025, 6, 1));
        Assert.Single(await Svc(s => s.GetEventsAsync(from: new DateTime(2025, 4, 1))));
    }

    [Fact]
    public async Task GetEvents_FiltersByTo()
    {
        await SeedEvent(new DateTime(2025, 1, 1));
        await SeedEvent(new DateTime(2025, 6, 1));
        Assert.Single(await Svc(s => s.GetEventsAsync(to: new DateTime(2025, 3, 1))));
    }

    [Fact]
    public async Task GetEvent_ReturnsEvent_WhenFound()
    {
        var seeded = await SeedEvent();
        var result = await Svc(s => s.GetEventAsync(seeded.Id));
        Assert.NotNull(result);
        Assert.Equal("Adoption Fair", result.Title);
    }

    [Fact]
    public async Task Schedule_CreatesAndReturns()
    {
        var start = new DateTime(2025, 9, 15, 10, 0, 0, DateTimeKind.Utc);
        var result = await Svc(s => s.ScheduleAsync(
            new CreateEventRequest("Fundraiser", start, start.AddHours(3), "Park", "Annual fundraiser", "Community", 5)));
        Assert.Equal("Fundraiser", result.Title);
        Assert.Equal("Community", result.EventType);
        Assert.Equal(5, result.AssignedVolunteers);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task Update_UpdatesEvent_WhenFound()
    {
        var seeded = await SeedEvent();
        var newStart = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var result = await Svc(s => s.UpdateAsync(
            new UpdateEventRequest(seeded.Id, "Updated Title", newStart, newStart.AddHours(2), "New Location", "Desc", "Fundraiser", 10)));
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Fundraiser", result.EventType);
        Assert.Equal(10, result.AssignedVolunteers);
    }

    [Fact]
    public async Task Update_ReturnsNull_WhenNotFound()
    {
        var start = DateTime.UtcNow;
        Assert.Null(await Svc(s => s.UpdateAsync(
            new UpdateEventRequest(99999, "X", start, start.AddHours(1), null, null, "General", null))));
    }

    [Fact]
    public async Task Delete_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedEvent();
        Assert.True(await Svc(s => s.DeleteAsync(seeded.Id)));
        Assert.Empty(await Svc(s => s.GetEventsAsync()));
    }

    [Fact]
    public async Task Delete_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.DeleteAsync(99999)));
    }
}
