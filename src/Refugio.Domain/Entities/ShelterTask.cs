namespace Refugio.Domain.Entities;

public class ShelterTask
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime DueDateTime { get; set; }
    public bool IsCompleted { get; set; }
    public string? AssignedTo { get; set; }
    public string? Location { get; set; }
    public int? AssignedVolunteerId { get; set; }
    public Volunteer? AssignedVolunteer { get; set; }
    public DateTime? DeletedAt { get; set; }
}
