namespace Refugio.Domain.Entities;

public class Volunteer : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string Role { get; set; } = "";
    public VolunteerStatus Status { get; set; } = VolunteerStatus.Active;
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public bool CanLogin { get; set; }
    public string? PasswordHash { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public enum VolunteerStatus { Active, Inactive, Pending }
