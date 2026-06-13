using Microsoft.Extensions.DependencyInjection;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Services;

namespace Refugio.Actors;

/// <summary>
/// Routes adoption-pipeline operations to IAdoptionService. Status changes stay inside
/// the service/aggregate: AdoptionStatusChanged is raised and dispatched after save.
/// </summary>
public sealed class AdoptionActor : ShelterActorBase<IAdoptionService>
{
    public AdoptionActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        Query<GetAdoptions>(async (s, m) => await s.GetAdoptionsAsync(m.Status));
        Query<GetAdoptionsPaged>(async (s, m) => await s.GetAdoptionsPagedAsync(m.Status, m.Page, m.PageSize));
        Query<GetAdoption>(async (s, m) => await s.GetAdoptionAsync(m.Id));
        Query<GetDeletedAdoptions>(async (s, _) => await s.GetDeletedAsync());
        Query<GetDeletedAdoption>(async (s, m) => await s.GetDeletedByIdAsync(m.Id));
        Command<CreateAdoptionRequest>(async (s, m) => await s.SubmitAsync(m));
        Command<UpdateAdoptionRequest>(async (s, m) => await s.UpdateAsync(m));
        Command<UpdateAdoptionStatusRequest>(async (s, m) => await s.ChangeStatusAsync(m));
        Command<AdvanceAdoption>(async (s, m) => await s.AdvanceAsync(m.Id));
        Command<DeleteAdoption>(async (s, m) => await s.DeleteAsync(m.Id));
        Command<RestoreAdoption>(async (s, m) => await s.RestoreAsync(m.Id));
        Command<PurgeAdoption>(async (s, m) => await s.PurgeAsync(m.Id));
        Query<GetAdoptionPhotos>(async (s, m) => await s.GetAdoptionPhotosAsync(m.AdoptionId));
        Command<AddAdoptionPhoto>(async (s, m) => await s.AddAdoptionPhotoAsync(m.AdoptionId, m.Url));
        Command<RemoveAdoptionPhoto>(async (s, m) => await s.RemoveAdoptionPhotoAsync(m.PhotoId));
    }
}
