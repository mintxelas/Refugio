using Akka.Actor;
using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Domain.Helpers;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Actors;

public class VolunteerActorTests : ActorTestBase
{
    private readonly IActorRef _actor;

    public VolunteerActorTests()
    {
        _actor = Sys.ActorOf(Props.Create(() => new VolunteerActor(_sf)));
    }

    private async Task<Volunteer> SeedVolunteer(
        string name = "Alice",
        VolunteerStatus status = VolunteerStatus.Active,
        bool canLogin = false,
        string? password = null)
        => await SeedAsync(db =>
        {
            var v = new Volunteer
            {
                Name = name, Email = $"{name.ToLower()}@test.com", Role = "Walker", Status = status,
                CanLogin = canLogin,
                PasswordHash = canLogin && password != null ? PasswordHelper.Hash(password) : null
            };
            db.Volunteers.Add(v);
            return v;
        });

    // ── Volunteers ────────────────────────────────────────────

    [Fact]
    public async Task GetAllVolunteers_ReturnsEmpty_WhenNone()
    {
        var result = await _actor.Ask<List<Volunteer>>(new GetAllVolunteers(), TimeSpan.FromSeconds(5));
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllVolunteers_ReturnsAll()
    {
        await SeedVolunteer("Alice");
        await SeedVolunteer("Bob");
        var result = await _actor.Ask<List<Volunteer>>(new GetAllVolunteers(), TimeSpan.FromSeconds(5));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllVolunteers_FiltersByStatus()
    {
        await SeedVolunteer("Alice", VolunteerStatus.Active);
        await SeedVolunteer("Bob", VolunteerStatus.Inactive);
        var result = await _actor.Ask<List<Volunteer>>(new GetAllVolunteers(VolunteerStatus.Active), TimeSpan.FromSeconds(5));
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public async Task GetVolunteerById_ReturnsVolunteer_WhenFound()
    {
        var seeded = await SeedVolunteer("Carol");
        var result = await _actor.Ask<Volunteer?>(new GetVolunteerById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("Carol", result.Name);
    }

    [Fact]
    public async Task CreateVolunteer_NoLogin_DoesNotHashPassword()
    {
        var result = await _actor.Ask<Volunteer>(
            new CreateVolunteer("Dave", "dave@test.com", "555-0001", "Driver", null, CanLogin: false),
            TimeSpan.FromSeconds(5));
        Assert.Equal("Dave", result.Name);
        Assert.False(result.CanLogin);
        Assert.Null(result.PasswordHash);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateVolunteer_WithLogin_HashesPassword()
    {
        var result = await _actor.Ask<Volunteer>(
            new CreateVolunteer("Eve", "eve@test.com", null, "Admin", null, CanLogin: true, Password: "secret123"),
            TimeSpan.FromSeconds(5));
        Assert.True(result.CanLogin);
        Assert.NotNull(result.PasswordHash);
        Assert.True(PasswordHelper.Verify("secret123", result.PasswordHash!));
    }

    [Fact]
    public async Task CreateVolunteer_WithLogin_EmptyPassword_NoHash()
    {
        var result = await _actor.Ask<Volunteer>(
            new CreateVolunteer("Frank", "frank@test.com", null, "Helper", null, CanLogin: true, Password: ""),
            TimeSpan.FromSeconds(5));
        Assert.Null(result.PasswordHash);
    }

    [Fact]
    public async Task UpdateVolunteer_UpdatesFields_WhenFound()
    {
        var seeded = await SeedVolunteer("Old");
        var result = await _actor.Ask<Volunteer?>(
            new UpdateVolunteer(seeded.Id, "New", "new@test.com", "555-9999", "Lead", "note", true, VolunteerStatus.Active, "newpass"),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("New", result.Name);
        Assert.Equal("new@test.com", result.Email);
        Assert.True(result.CanLogin);
        Assert.NotNull(result.PasswordHash);
    }

    [Fact]
    public async Task UpdateVolunteer_ReturnsNull_WhenNotFound()
    {
        var result = await _actor.Ask<Volunteer?>(
            new UpdateVolunteer(99999, "X", "x@x.com", null, "Y", null, false, VolunteerStatus.Active),
            TimeSpan.FromSeconds(5));
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateVolunteer_ClearsPassword_WhenCanLoginFalse()
    {
        var seeded = await SeedVolunteer("WithLogin", canLogin: true, password: "oldpw");
        var result = await _actor.Ask<Volunteer?>(
            new UpdateVolunteer(seeded.Id, "WithLogin", seeded.Email, null, "Walker", null, false, VolunteerStatus.Active),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.False(result.CanLogin);
        Assert.Null(result.PasswordHash);
    }

    [Fact]
    public async Task UpdateVolunteer_NoNewPassword_KeepsExistingHash()
    {
        var seeded = await SeedVolunteer("Keeper", canLogin: true, password: "keep123");
        var oldHash = seeded.PasswordHash;
        var result = await _actor.Ask<Volunteer?>(
            new UpdateVolunteer(seeded.Id, "Keeper", seeded.Email, null, "Walker", null, true, VolunteerStatus.Active, NewPassword: null),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        // No new password → hash unchanged
        Assert.Equal(oldHash, result.PasswordHash);
    }

    [Fact]
    public async Task UpdateVolunteerStatus_UpdatesStatus_WhenFound()
    {
        var seeded = await SeedVolunteer("G", VolunteerStatus.Active);
        var result = await _actor.Ask<Volunteer?>(
            new UpdateVolunteerStatus(seeded.Id, VolunteerStatus.Inactive),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal(VolunteerStatus.Inactive, result.Status);
    }

    [Fact]
    public async Task UpdateVolunteerStatus_ReturnsNull_WhenNotFound()
    {
        var result = await _actor.Ask<Volunteer?>(
            new UpdateVolunteerStatus(99999, VolunteerStatus.Inactive),
            TimeSpan.FromSeconds(5));
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteVolunteer_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedVolunteer("Del");
        var result = await _actor.Ask<bool>(new DeleteVolunteer(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteVolunteer_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new DeleteVolunteer(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    [Fact]
    public async Task LoginVolunteer_ReturnsVolunteer_WhenCredentialsValid()
    {
        await SeedVolunteer("Hanna", canLogin: true, password: "pass1234");
        var result = await _actor.Ask<Volunteer?>(
            new LoginVolunteer("hanna@test.com", "pass1234"),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("Hanna", result.Name);
    }

    [Fact]
    public async Task LoginVolunteer_ReturnsNull_WhenWrongPassword()
    {
        await SeedVolunteer("Ivan", canLogin: true, password: "correct");
        var result = await _actor.Ask<Volunteer?>(
            new LoginVolunteer("ivan@test.com", "wrong"),
            TimeSpan.FromSeconds(5));
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginVolunteer_ReturnsNull_WhenUserNotFound()
    {
        var result = await _actor.Ask<Volunteer?>(
            new LoginVolunteer("nobody@test.com", "pass"),
            TimeSpan.FromSeconds(5));
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginVolunteer_ReturnsNull_WhenCanLoginFalse()
    {
        await SeedAsync(db =>
        {
            var v = new Volunteer { Name = "NoLogin", Email = "nologin@test.com", Role = "Helper", CanLogin = false };
            db.Volunteers.Add(v);
            return v;
        });
        var result = await _actor.Ask<Volunteer?>(
            new LoginVolunteer("nologin@test.com", "any"),
            TimeSpan.FromSeconds(5));
        Assert.Null(result);
    }

    [Fact]
    public async Task ChangeVolunteerPassword_ReturnsTrue_WhenValid()
    {
        var seeded = await SeedVolunteer("Jake", canLogin: true, password: "oldPass");
        var result = await _actor.Ask<bool>(
            new ChangeVolunteerPassword(seeded.Id, "oldPass", "newPass"),
            TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task ChangeVolunteerPassword_ReturnsFalse_WhenWrongCurrent()
    {
        var seeded = await SeedVolunteer("Kim", canLogin: true, password: "correct");
        var result = await _actor.Ask<bool>(
            new ChangeVolunteerPassword(seeded.Id, "wrong", "newPass"),
            TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    [Fact]
    public async Task ChangeVolunteerPassword_ReturnsFalse_WhenNoHash()
    {
        var seeded = await SeedVolunteer("NoPass", canLogin: false);
        var result = await _actor.Ask<bool>(
            new ChangeVolunteerPassword(seeded.Id, "any", "new"),
            TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    // ── Events ────────────────────────────────────────────────

    private async Task<ShelterEvent> SeedEvent(DateTime? start = null)
        => await SeedAsync(db =>
        {
            var ev = new ShelterEvent
            {
                Title = "Adoption Fair", EventType = "Adoption",
                StartDateTime = start ?? DateTime.UtcNow,
                EndDateTime = (start ?? DateTime.UtcNow).AddHours(4)
            };
            db.Events.Add(ev);
            return ev;
        });

    [Fact]
    public async Task GetAllEvents_ReturnsEmpty_WhenNone()
    {
        var result = await _actor.Ask<List<ShelterEvent>>(new GetAllEvents(), TimeSpan.FromSeconds(5));
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllEvents_ReturnsAll()
    {
        await SeedEvent(new DateTime(2025, 3, 1));
        await SeedEvent(new DateTime(2025, 6, 1));
        var result = await _actor.Ask<List<ShelterEvent>>(new GetAllEvents(), TimeSpan.FromSeconds(5));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllEvents_FiltersByFrom()
    {
        await SeedEvent(new DateTime(2025, 1, 1));
        await SeedEvent(new DateTime(2025, 6, 1));
        var result = await _actor.Ask<List<ShelterEvent>>(
            new GetAllEvents(From: new DateTime(2025, 4, 1)),
            TimeSpan.FromSeconds(5));
        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllEvents_FiltersByTo()
    {
        await SeedEvent(new DateTime(2025, 1, 1));
        await SeedEvent(new DateTime(2025, 6, 1));
        var result = await _actor.Ask<List<ShelterEvent>>(
            new GetAllEvents(To: new DateTime(2025, 3, 1)),
            TimeSpan.FromSeconds(5));
        Assert.Single(result);
    }

    [Fact]
    public async Task GetEventById_ReturnsEvent_WhenFound()
    {
        var seeded = await SeedEvent();
        var result = await _actor.Ask<ShelterEvent?>(new GetEventById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("Adoption Fair", result.Title);
    }

    [Fact]
    public async Task CreateEvent_CreatesAndReturns()
    {
        var start = new DateTime(2025, 9, 15, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(3);
        var result = await _actor.Ask<ShelterEvent>(
            new CreateEvent("Fundraiser", start, end, "Park", "Annual fundraiser", "Community", 5),
            TimeSpan.FromSeconds(5));
        Assert.Equal("Fundraiser", result.Title);
        Assert.Equal("Community", result.EventType);
        Assert.Equal(5, result.AssignedVolunteers);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task UpdateEvent_UpdatesEvent_WhenFound()
    {
        var seeded = await SeedEvent();
        var newStart = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var result = await _actor.Ask<ShelterEvent?>(
            new UpdateEvent(seeded.Id, "Updated Title", newStart, newStart.AddHours(2), "New Location", "Desc", "Fundraiser", 10),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Fundraiser", result.EventType);
        Assert.Equal(10, result.AssignedVolunteers);
    }

    [Fact]
    public async Task UpdateEvent_ReturnsNull_WhenNotFound()
    {
        var start = DateTime.UtcNow;
        var result = await _actor.Ask<ShelterEvent?>(
            new UpdateEvent(99999, "X", start, start.AddHours(1), null, null, "General", null),
            TimeSpan.FromSeconds(5));
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteEvent_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedEvent();
        var result = await _actor.Ask<bool>(new DeleteEvent(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteEvent_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new DeleteEvent(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    // ── GetDeletedVolunteers / RestoreVolunteer ────────────────

    [Fact]
    public async Task GetDeletedVolunteers_ReturnsEmpty_WhenNoneDeleted()
    {
        await SeedVolunteer("LiveVol");
        var deleted = await _actor.Ask<List<Volunteer>>(new GetDeletedVolunteers(), TimeSpan.FromSeconds(5));
        Assert.Empty(deleted);
    }

    [Fact]
    public async Task GetDeletedVolunteers_ReturnsOnlyDeleted()
    {
        await SeedVolunteer("ActiveVol");
        await SeedAsync(db =>
        {
            var v = new Volunteer { Name = "GoneVol", Email = "gone@vol.com", Role = "Walker", DeletedAt = DateTime.UtcNow };
            db.Volunteers.Add(v);
            return v;
        });
        var deleted = await _actor.Ask<List<Volunteer>>(new GetDeletedVolunteers(), TimeSpan.FromSeconds(5));
        Assert.Single(deleted);
        Assert.Equal("GoneVol", deleted[0].Name);
    }

    [Fact]
    public async Task RestoreVolunteer_ReturnsTrue_AndAppearsInActiveQuery()
    {
        var seeded = await SeedAsync(db =>
        {
            var v = new Volunteer { Name = "ReviveVol", Email = "revive@vol.com", Role = "Helper", DeletedAt = DateTime.UtcNow };
            db.Volunteers.Add(v);
            return v;
        });
        var result = await _actor.Ask<bool>(new RestoreVolunteer(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
        var volunteer = await _actor.Ask<Volunteer?>(new GetVolunteerById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(volunteer);
        Assert.Null(volunteer.DeletedAt);
    }

    [Fact]
    public async Task RestoreVolunteer_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new RestoreVolunteer(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }
}
