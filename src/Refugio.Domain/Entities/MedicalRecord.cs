namespace Refugio.Domain.Entities;

public class MedicalRecord
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public Dog Dog { get; set; } = null!;
    public DateTime VisitDate { get; set; } = DateTime.UtcNow;
    public string VetName { get; set; } = "";
    public string Diagnosis { get; set; } = "";
    public string Treatment { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime? NextVisitDate { get; set; }
    public DateTime? DeletedAt { get; set; }
}
