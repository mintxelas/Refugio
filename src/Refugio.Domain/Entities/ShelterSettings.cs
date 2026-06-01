namespace Refugio.Domain.Entities;

public class ShelterSettings : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = "Haven Sanctuary";
    public string? Phrase { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime? DeletedAt { get; set; }
}
