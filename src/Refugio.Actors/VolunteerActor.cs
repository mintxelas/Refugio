using Microsoft.Extensions.DependencyInjection;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Services;

namespace Refugio.Actors;

/// <summary>Routes volunteer operations (including credentials) to IVolunteerService.</summary>
public sealed class VolunteerActor : ShelterActorBase<IVolunteerService>
{
    public VolunteerActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        Query<GetVolunteers>(async (s, m) => await s.GetVolunteersAsync(m.Status));
        Query<GetVolunteersPaged>(async (s, m) => await s.GetVolunteersPagedAsync(m.Status, m.Page, m.PageSize));
        Query<GetVolunteer>(async (s, m) => await s.GetVolunteerAsync(m.Id));
        Query<GetDeletedVolunteers>(async (s, _) => await s.GetDeletedAsync());
        Query<GetDeletedVolunteer>(async (s, m) => await s.GetDeletedByIdAsync(m.Id));
        Query<Login>(async (s, m) => await s.LoginAsync(m.Email, m.Password));
        Command<CreateVolunteerRequest>(async (s, m) => await s.RegisterAsync(m));
        Command<UpdateVolunteerRequest>(async (s, m) => await s.UpdateAsync(m));
        Command<ChangeVolunteerStatus>(async (s, m) => await s.ChangeStatusAsync(m.Id, m.Status));
        Command<ChangePassword>(async (s, m) => await s.ChangePasswordAsync(m.Id, m.CurrentPassword, m.NewPassword));
        Command<SetVolunteerPhoto>(async (s, m) => await s.SetPhotoAsync(m.Id, m.PhotoUrl));
        Command<DeleteVolunteer>(async (s, m) => await s.DeleteAsync(m.Id));
        Command<RestoreVolunteer>(async (s, m) => await s.RestoreAsync(m.Id));
        Command<PurgeVolunteer>(async (s, m) => await s.PurgeAsync(m.Id));
    }
}
