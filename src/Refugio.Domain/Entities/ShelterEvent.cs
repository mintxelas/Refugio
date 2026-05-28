namespace Refugio.Domain.Entities;

public class ShelterEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string EventType { get; set; } = "General";
    public int? AssignedVolunteers { get; set; }
    public DateTime? DeletedAt { get; set; }
}
