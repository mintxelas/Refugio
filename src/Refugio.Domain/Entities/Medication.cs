namespace Refugio.Domain.Entities;

public class Medication
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public Dog Dog { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Frequency { get; set; } = "";
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
}
