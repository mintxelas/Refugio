using Refugio.Domain.Entities;

namespace Refugio.Actors.Messages;

// Volunteer-area actor messages. Register/Update reuse the request records.
public sealed record GetVolunteers(VolunteerStatus? Status);
public sealed record GetVolunteersPaged(VolunteerStatus? Status, int Page, int PageSize);
public sealed record GetVolunteer(int Id);
public sealed record ChangeVolunteerStatus(int Id, VolunteerStatus Status);
public sealed record Login(string Email, string Password);
public sealed record ChangePassword(int Id, string CurrentPassword, string NewPassword);
public sealed record SetVolunteerPhoto(int Id, string? PhotoUrl);
public sealed record DeleteVolunteer(int Id);
public sealed record RestoreVolunteer(int Id);
public sealed record PurgeVolunteer(int Id);
public sealed record GetDeletedVolunteers;
public sealed record GetDeletedVolunteer(int Id);
