using Refugio.Domain.Entities;

namespace Refugio.Application.Messages;

public record GetAllVolunteers(VolunteerStatus? Status = null) : IVolunteerMessage;
public record GetVolunteersPaged(VolunteerStatus? Status, int Page, int PageSize = 25) : IVolunteerMessage;
public record GetVolunteerById(int Id) : IVolunteerMessage;
public record CreateVolunteer(string Name, string Email, string? Phone, string Role, string? Notes, bool CanLogin = false, string? Password = null, string? PreferredLanguage = null) : IVolunteerMessage;
public record UpdateVolunteerStatus(int Id, VolunteerStatus Status) : IVolunteerMessage;
public record DeleteVolunteer(int Id) : IVolunteerMessage;
public record UpdateVolunteer(int Id, string Name, string Email, string? Phone, string Role, string? Notes, bool CanLogin, VolunteerStatus Status, string? NewPassword = null, string? PreferredLanguage = null) : IVolunteerMessage;
public record LoginVolunteer(string Email, string Password) : IVolunteerMessage;
public record ChangeVolunteerPassword(int Id, string CurrentPassword, string NewPassword) : IVolunteerMessage;
public record UpdateVolunteerPhoto(int Id, string? PhotoUrl) : IVolunteerMessage;

public record GetAllEvents(DateTime? From = null, DateTime? To = null) : IVolunteerMessage;
public record GetEventById(int Id) : IVolunteerMessage;
public record CreateEvent(string Title, DateTime StartDateTime, DateTime EndDateTime, string? Location, string? Description, string EventType, int? AssignedVolunteers) : IVolunteerMessage;
public record UpdateEvent(int Id, string Title, DateTime StartDateTime, DateTime EndDateTime, string? Location, string? Description, string EventType, int? AssignedVolunteers) : IVolunteerMessage;
public record DeleteEvent(int Id) : IVolunteerMessage;

public record GetDeletedVolunteers() : IVolunteerMessage;
public record RestoreVolunteer(int Id) : IVolunteerMessage;
public record GetDeletedVolunteerById(int Id) : IVolunteerMessage;
public record PermanentDeleteVolunteer(int Id) : IVolunteerMessage;

public record GetVolunteerCounts() : IVolunteerMessage;
public record VolunteerCounts(int Total, int Active, int Pending);
