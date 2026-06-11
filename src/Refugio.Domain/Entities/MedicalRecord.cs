using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

/// <summary>Child entity of the Dog aggregate: one vet visit.</summary>
public class MedicalRecord : Entity, IHasDogId
{
    public int DogId { get; private set; }
    public Dog Dog { get; private set; } = null!;
    public DateTime VisitDate { get; private set; } = DateTime.UtcNow;
    public string VetName { get; private set; } = "";
    public string Diagnosis { get; private set; } = "";
    public string Treatment { get; private set; } = "";
    public string? Notes { get; private set; }
    public DateTime? NextVisitDate { get; private set; }

    private MedicalRecord() { }

    public static MedicalRecord Create(
        int dogId, string vetName, string diagnosis, string treatment,
        string? notes, DateTime? nextVisitDate, DateTime? visitDate = null) => new()
    {
        DogId = dogId,
        VetName = vetName,
        Diagnosis = diagnosis,
        Treatment = treatment,
        Notes = notes,
        NextVisitDate = nextVisitDate,
        VisitDate = visitDate ?? DateTime.UtcNow
    };

    public void Update(
        string vetName, string diagnosis, string treatment, string? notes,
        DateTime visitDate, DateTime? nextVisitDate)
    {
        VetName = vetName;
        Diagnosis = diagnosis;
        Treatment = treatment;
        Notes = notes;
        VisitDate = visitDate;
        NextVisitDate = nextVisitDate;
    }
}
