using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

/// <summary>Aggregate root for a calendar event (walks, adoption days, vet checkups…).</summary>
public class ShelterEvent : Entity, IAggregateRoot
{
    public string Title { get; private set; } = "";
    public DateTime StartDateTime { get; private set; }
    public DateTime EndDateTime { get; private set; }
    public string? Location { get; private set; }
    public string? Description { get; private set; }
    public string EventType { get; private set; } = "General";
    public int? AssignedVolunteers { get; private set; }

    private ShelterEvent() { }

    public static ShelterEvent Schedule(
        string title, DateTime startDateTime, DateTime endDateTime,
        string? location = null, string? description = null,
        string eventType = "General", int? assignedVolunteers = null) => new()
    {
        Title = title,
        StartDateTime = startDateTime,
        EndDateTime = endDateTime,
        Location = location,
        Description = description,
        EventType = eventType,
        AssignedVolunteers = assignedVolunteers
    };

    public void Update(
        string title, DateTime startDateTime, DateTime endDateTime,
        string? location, string? description, string eventType, int? assignedVolunteers)
    {
        Title = title;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        Location = location;
        Description = description;
        EventType = eventType;
        AssignedVolunteers = assignedVolunteers;
    }
}
