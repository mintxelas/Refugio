using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Domain.Helpers;
using Refugio.Infrastructure.Data;

namespace Refugio.Application.Actors;

public class VolunteerActor : ReceiveActor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public VolunteerActor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        ReceiveAsync<GetAllVolunteers>(Handle);
        ReceiveAsync<GetVolunteersPaged>(Handle);
        ReceiveAsync<GetVolunteerById>(Handle);
        ReceiveAsync<CreateVolunteer>(Handle);
        ReceiveAsync<UpdateVolunteer>(Handle);
        ReceiveAsync<UpdateVolunteerStatus>(Handle);
        ReceiveAsync<DeleteVolunteer>(Handle);
        ReceiveAsync<LoginVolunteer>(Handle);
        ReceiveAsync<ChangeVolunteerPassword>(Handle);
        ReceiveAsync<GetAllEvents>(Handle);
        ReceiveAsync<GetEventById>(Handle);
        ReceiveAsync<CreateEvent>(Handle);
        ReceiveAsync<UpdateEvent>(Handle);
        ReceiveAsync<DeleteEvent>(Handle);
        ReceiveAsync<GetDeletedVolunteers>(Handle);
        ReceiveAsync<RestoreVolunteer>(Handle);
    }

    private ShelterDbContext Db(IServiceScope s) => s.ServiceProvider.GetRequiredService<ShelterDbContext>();

    private async Task Handle(GetAllVolunteers msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var q = Db(scope).Volunteers.AsQueryable();
        if (msg.Status.HasValue) q = q.Where(v => v.Status == msg.Status);
        Sender.Tell(await q.OrderBy(v => v.Name).ToListAsync());
    }

    private async Task Handle(GetVolunteersPaged msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var q = Db(scope).Volunteers.AsQueryable();
        if (msg.Status.HasValue) q = q.Where(v => v.Status == msg.Status);
        q = q.OrderBy(v => v.Name);
        var total = await q.CountAsync();
        var items = await q.Skip((msg.Page - 1) * msg.PageSize).Take(msg.PageSize).ToListAsync();
        Sender.Tell(new VolunteerPage(items, total, msg.Page, msg.PageSize));
    }

    private async Task Handle(GetVolunteerById msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Volunteers.FirstOrDefaultAsync(v => v.Id == msg.Id));
    }

    private async Task Handle(CreateVolunteer msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var v = new Volunteer
        {
            Name = msg.Name, Email = msg.Email, Phone = msg.Phone, Role = msg.Role, Notes = msg.Notes,
            CanLogin = msg.CanLogin,
            PasswordHash = msg.CanLogin && !string.IsNullOrWhiteSpace(msg.Password)
                ? PasswordHelper.Hash(msg.Password)
                : null
        };
        db.Volunteers.Add(v);
        await db.SaveChangesAsync();
        Sender.Tell(v);
    }

    private async Task Handle(UpdateVolunteer msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var v = await db.Volunteers.FindAsync(msg.Id);
        if (v is null) { Sender.Tell((Volunteer?)null); return; }
        v.Name = msg.Name;
        v.Email = msg.Email;
        v.Phone = msg.Phone;
        v.Role = msg.Role;
        v.Notes = msg.Notes;
        v.Status = msg.Status;
        v.CanLogin = msg.CanLogin;
        if (msg.CanLogin && !string.IsNullOrWhiteSpace(msg.NewPassword))
            v.PasswordHash = PasswordHelper.Hash(msg.NewPassword);
        else if (!msg.CanLogin)
            v.PasswordHash = null;
        await db.SaveChangesAsync();
        Sender.Tell(v);
    }

    private async Task Handle(UpdateVolunteerStatus msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var v = await db.Volunteers.FindAsync(msg.Id);
        if (v is null) { Sender.Tell((Volunteer?)null); return; }
        v.Status = msg.Status;
        await db.SaveChangesAsync();
        Sender.Tell(v);
    }

    private async Task Handle(DeleteVolunteer msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var v = await db.Volunteers.FindAsync(msg.Id);
        if (v is null) { Sender.Tell(false); return; }
        v.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(LoginVolunteer msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var v = await Db(scope).Volunteers
            .FirstOrDefaultAsync(x => x.Email == msg.Email && x.CanLogin);
        if (v?.PasswordHash is null || !PasswordHelper.Verify(msg.Password, v.PasswordHash))
        {
            Sender.Tell((Volunteer?)null);
            return;
        }
        Sender.Tell(v);
    }

    private async Task Handle(ChangeVolunteerPassword msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var v = await db.Volunteers.FindAsync(msg.Id);
        if (v?.PasswordHash is null || !PasswordHelper.Verify(msg.CurrentPassword, v.PasswordHash))
        {
            Sender.Tell(false);
            return;
        }
        v.PasswordHash = PasswordHelper.Hash(msg.NewPassword);
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(GetEventById msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Events.FirstOrDefaultAsync(e => e.Id == msg.Id));
    }

    private async Task Handle(UpdateEvent msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var ev = await db.Events.FindAsync(msg.Id);
        if (ev is null) { Sender.Tell((ShelterEvent?)null); return; }
        ev.Title = msg.Title;
        ev.StartDateTime = msg.StartDateTime;
        ev.EndDateTime = msg.EndDateTime;
        ev.Location = msg.Location;
        ev.Description = msg.Description;
        ev.EventType = msg.EventType;
        ev.AssignedVolunteers = msg.AssignedVolunteers;
        await db.SaveChangesAsync();
        Sender.Tell(ev);
    }

    private async Task Handle(DeleteEvent msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var ev = await db.Events.FindAsync(msg.Id);
        if (ev is null) { Sender.Tell(false); return; }
        ev.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(GetAllEvents msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var q = Db(scope).Events.AsQueryable();
        if (msg.From.HasValue) q = q.Where(e => e.StartDateTime >= msg.From);
        if (msg.To.HasValue) q = q.Where(e => e.StartDateTime <= msg.To);
        Sender.Tell(await q.OrderBy(e => e.StartDateTime).ToListAsync());
    }

    private async Task Handle(GetDeletedVolunteers msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Volunteers.IgnoreQueryFilters()
            .Where(v => v.DeletedAt != null)
            .OrderByDescending(v => v.DeletedAt)
            .ToListAsync());
    }

    private async Task Handle(RestoreVolunteer msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var v = await db.Volunteers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == msg.Id);
        if (v is null) { Sender.Tell(false); return; }
        v.DeletedAt = null;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(CreateEvent msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var ev = new ShelterEvent
        {
            Title = msg.Title, StartDateTime = msg.StartDateTime, EndDateTime = msg.EndDateTime,
            Location = msg.Location, Description = msg.Description, EventType = msg.EventType,
            AssignedVolunteers = msg.AssignedVolunteers
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        Sender.Tell(ev);
    }
}
