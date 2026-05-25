namespace Refugio.Domain.Entities;

public class Dog
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Breed { get; set; } = "";
    public int AgeMonths { get; set; }
    public string Gender { get; set; } = "";
    public DogStatus Status { get; set; } = DogStatus.Available;
    public string? PhotoUrl { get; set; }
    public string? Traits { get; set; }
    public string? Notes { get; set; }
    public DateTime ArrivalDate { get; set; } = DateTime.UtcNow;
    public decimal WeightKg { get; set; }

    public ICollection<MedicalRecord> MedicalRecords { get; set; } = [];
    public ICollection<Medication> Medications { get; set; } = [];
    public ICollection<Adoption> Adoptions { get; set; } = [];
}

public enum DogStatus
{
    Available,
    Adopted,
    Foster,
    Medical,
    Quarantine
}
