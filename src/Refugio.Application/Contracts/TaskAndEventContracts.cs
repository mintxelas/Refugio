namespace Refugio.Application.Contracts;

public record ShelterTaskDto(
    int Id, string Title, string? Notes, DateTime DueDateTime, bool IsCompleted,
    string? Location, int? AssignedVolunteerId, VolunteerDto? AssignedVolunteer = null);

public record CreateTaskRequest(
    string Title, DateTime DueDateTime, string? Notes, int? AssignedVolunteerId, string? Location);

public record ShelterEventDto(
    int Id, string Title, DateTime StartDateTime, DateTime EndDateTime,
    string? Location, string? Description, string EventType, int? AssignedVolunteers);

public record CreateEventRequest(
    string Title, DateTime StartDateTime, DateTime EndDateTime,
    string? Location, string? Description, string EventType, int? AssignedVolunteers);

public record UpdateEventRequest(
    int Id, string Title, DateTime StartDateTime, DateTime EndDateTime,
    string? Location, string? Description, string EventType, int? AssignedVolunteers);
