using Refugio.Domain.Entities;

namespace Refugio.Actors.Messages;

// Adoption-area actor messages. Submit/Update/ChangeStatus reuse the request records.
public sealed record GetAdoptions(AdoptionStatus? Status);
public sealed record GetAdoptionsPaged(AdoptionStatus? Status, int Page, int PageSize);
public sealed record GetAdoption(int Id);
public sealed record AdvanceAdoption(int Id);
public sealed record DeleteAdoption(int Id);
public sealed record RestoreAdoption(int Id);
public sealed record PurgeAdoption(int Id);
public sealed record GetDeletedAdoptions;
public sealed record GetDeletedAdoption(int Id);
public sealed record GetAdoptionPhotos(int AdoptionId);
public sealed record AddAdoptionPhoto(int AdoptionId, string Url);
public sealed record RemoveAdoptionPhoto(int PhotoId);
