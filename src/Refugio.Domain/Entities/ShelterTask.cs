using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

/// <summary>Aggregate root for a day-to-day shelter task, optionally assigned to a volunteer.</summary>
public class ShelterTask : Entity, IAggregateRoot
{
    public string Title { get; private set; } = "";
    public string? Notes { get; private set; }
    public DateTime DueDateTime { get; private set; }
    public bool IsCompleted { get; private set; }
    public string? Location { get; private set; }
    public int? AssignedVolunteerId { get; private set; }
    public Volunteer? AssignedVolunteer { get; private set; }

    private ShelterTask() { }

    public static ShelterTask Create(
        string title, DateTime dueDateTime, string? notes = null,
        string? location = null, int? assignedVolunteerId = null) => new()
    {
        Title = title,
        DueDateTime = dueDateTime,
        Notes = notes,
        Location = location,
        AssignedVolunteerId = assignedVolunteerId
    };

    public void Complete() => IsCompleted = true;
}
