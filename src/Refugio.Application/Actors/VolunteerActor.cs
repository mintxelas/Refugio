using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Domain.Helpers;

namespace Refugio.Application.Actors;

public class VolunteerActor : ShelterActorBase
{
    public VolunteerActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        ReceiveAsync<GetAllVolunteers>(Handle);
        ReceiveAsync<GetVolunteersPaged>(Handle);
        ReceiveAsync<GetVolunteerById>(Handle);
        ReceiveAsync<CreateVolunteer>(Handle);
        ReceiveAsync<UpdateVolunteer>(Handle);
        ReceiveAsync<UpdateVolunteerStatus>(Handle);
        ReceiveAsync<DeleteVolunteer>(msg => SoftDelete<Volunteer>(msg.Id));
        ReceiveAsync<LoginVolunteer>(Handle);
        ReceiveAsync<ChangeVolunteerPassword>(Handle);
        ReceiveAsync<UpdateVolunteerPhoto>(Handle);
        ReceiveAsync<GetAllEvents>(Handle);
        ReceiveAsync<GetEventById>(Handle);
        ReceiveAsync<CreateEvent>(Handle);
        ReceiveAsync<UpdateEvent>(Handle);
        ReceiveAsync<DeleteEvent>(msg => SoftDelete<ShelterEvent>(msg.Id));
        ReceiveAsync<GetDeletedVolunteers>(_ => GetDeleted<Volunteer>());
        ReceiveAsync<RestoreVolunteer>(msg => Restore<Volunteer>(msg.Id));
        ReceiveAsync<GetDeletedVolunteerById>(msg => GetDeletedById<Volunteer>(msg.Id));
        ReceiveAsync<PermanentDeleteVolunteer>(msg => PermanentDelete<Volunteer>(msg.Id));
        ReceiveAsync<GetVolunteerCounts>(Handle);
    }

    private Task Handle(GetAllVolunteers msg) => WithDb(async db =>
    {
        var q = db.Volunteers.AsQueryable();
        if (msg.Status.HasValue) q = q.Where(v => v.Status == msg.Status);
        Sender.Tell(await q.OrderBy(v => v.Name).ToListAsync());
    });

    private Task Handle(GetVolunteersPaged msg) => WithDb(async db =>
    {
        var q = db.Volunteers.AsQueryable();
        if (msg.Status.HasValue) q = q.Where(v => v.Status == msg.Status);
        Sender.Tell(await q.OrderBy(v => v.Name).ToPageAsync(msg.Page, msg.PageSize));
    });

    private Task Handle(GetVolunteerById msg) => WithDb(async db =>
        Sender.Tell(await db.Volunteers.FirstOrDefaultAsync(v => v.Id == msg.Id)));

    private Task Handle(CreateVolunteer msg) => WithDb(async db =>
    {
        var v = new Volunteer
        {
            Name = msg.Name, Email = msg.Email, Phone = msg.Phone, Role = msg.Role, Notes = msg.Notes,
            CanLogin = msg.CanLogin,
            PreferredLanguage = msg.CanLogin ? msg.PreferredLanguage : null,
            PasswordHash = msg.CanLogin && !string.IsNullOrWhiteSpace(msg.Password)
                ? PasswordHelper.Hash(msg.Password)
                : null
        };
        db.Volunteers.Add(v);
        await db.SaveChangesAsync();
        Sender.Tell(v);
    });

    private Task Handle(UpdateVolunteer msg) => WithDb(async db =>
    {
        var v = await db.Volunteers.FindAsync(msg.Id);
        if (v is null) { Sender.Tell((Volunteer?)null); return; }
        v.Name = msg.Name;
        v.Email = msg.Email;
        v.Phone = msg.Phone;
        v.Role = msg.Role;
        v.Notes = msg.Notes;
        v.Status = msg.Status;
        v.CanLogin = msg.CanLogin;
        v.PreferredLanguage = msg.CanLogin ? msg.PreferredLanguage : null;
        if (msg.CanLogin && !string.IsNullOrWhiteSpace(msg.NewPassword))
            v.PasswordHash = PasswordHelper.Hash(msg.NewPassword);
        else if (!msg.CanLogin)
            v.PasswordHash = null;
        await db.SaveChangesAsync();
        Sender.Tell(v);
    });

    private Task Handle(UpdateVolunteerStatus msg) => WithDb(async db =>
    {
        var v = await db.Volunteers.FindAsync(msg.Id);
        if (v is null) { Sender.Tell((Volunteer?)null); return; }
        v.Status = msg.Status;
        await db.SaveChangesAsync();
        Sender.Tell(v);
    });

    private Task Handle(LoginVolunteer msg) => WithDb(async db =>
    {
        var v = await db.Volunteers.FirstOrDefaultAsync(x => x.Email == msg.Email && x.CanLogin);
        if (v?.PasswordHash is null || !PasswordHelper.Verify(msg.Password, v.PasswordHash))
        {
            Sender.Tell((Volunteer?)null);
            return;
        }
        Sender.Tell(v);
    });

    private Task Handle(ChangeVolunteerPassword msg) => WithDb(async db =>
    {
        var v = await db.Volunteers.FindAsync(msg.Id);
        if (v?.PasswordHash is null || !PasswordHelper.Verify(msg.CurrentPassword, v.PasswordHash))
        {
            Sender.Tell(false);
            return;
        }
        v.PasswordHash = PasswordHelper.Hash(msg.NewPassword);
        await db.SaveChangesAsync();
        Sender.Tell(true);
    });

    private Task Handle(UpdateVolunteerPhoto msg) => WithDb(async db =>
    {
        var v = await db.Volunteers.FindAsync(msg.Id);
        if (v is null) { Sender.Tell(false); return; }
        v.PhotoUrl = msg.PhotoUrl;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    });

    private Task Handle(GetAllEvents msg) => WithDb(async db =>
    {
        var q = db.Events.AsQueryable();
        if (msg.From.HasValue) q = q.Where(e => e.StartDateTime >= msg.From);
        if (msg.To.HasValue) q = q.Where(e => e.StartDateTime <= msg.To);
        Sender.Tell(await q.OrderBy(e => e.StartDateTime).ToListAsync());
    });

    private Task Handle(GetEventById msg) => WithDb(async db =>
        Sender.Tell(await db.Events.FirstOrDefaultAsync(e => e.Id == msg.Id)));

    private Task Handle(CreateEvent msg) => WithDb(async db =>
    {
        var ev = new ShelterEvent
        {
            Title = msg.Title, StartDateTime = msg.StartDateTime, EndDateTime = msg.EndDateTime,
            Location = msg.Location, Description = msg.Description, EventType = msg.EventType,
            AssignedVolunteers = msg.AssignedVolunteers
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        Sender.Tell(ev);
    });

    private Task Handle(UpdateEvent msg) => WithDb(async db =>
    {
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
    });

    private Task Handle(GetVolunteerCounts msg) => WithDb(async db =>
    {
        var total = await db.Volunteers.CountAsync();
        var active = await db.Volunteers.CountAsync(v => v.Status == VolunteerStatus.Active);
        var pending = await db.Volunteers.CountAsync(v => v.Status == VolunteerStatus.Pending);
        Sender.Tell(new VolunteerCounts(total, active, pending));
    });
}
