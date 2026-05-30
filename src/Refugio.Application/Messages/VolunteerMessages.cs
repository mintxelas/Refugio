using Refugio.Domain.Entities;

namespace Refugio.Application.Messages;

public record GetAllVolunteers(VolunteerStatus? Status = null);
public record GetVolunteersPaged(VolunteerStatus? Status, int Page, int PageSize = 25);
public record VolunteerPage(List<Volunteer> Items, int TotalCount, int Page, int PageSize);
public record GetVolunteerById(int Id);
public record CreateVolunteer(string Name, string Email, string? Phone, string Role, string? Notes, bool CanLogin = false, string? Password = null);
public record UpdateVolunteerStatus(int Id, VolunteerStatus Status);
public record DeleteVolunteer(int Id);
public record UpdateVolunteer(int Id, string Name, string Email, string? Phone, string Role, string? Notes, bool CanLogin, VolunteerStatus Status, string? NewPassword = null);
public record LoginVolunteer(string Email, string Password);
public record ChangeVolunteerPassword(int Id, string CurrentPassword, string NewPassword);

public record GetAllEvents(DateTime? From = null, DateTime? To = null);
public record GetEventById(int Id);
public record CreateEvent(string Title, DateTime StartDateTime, DateTime EndDateTime, string? Location, string? Description, string EventType, int? AssignedVolunteers);
public record UpdateEvent(int Id, string Title, DateTime StartDateTime, DateTime EndDateTime, string? Location, string? Description, string EventType, int? AssignedVolunteers);
public record DeleteEvent(int Id);

public record GetDeletedVolunteers();
public record RestoreVolunteer(int Id);
