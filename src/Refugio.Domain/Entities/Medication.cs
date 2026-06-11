using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

/// <summary>Child entity of the Dog aggregate: an ongoing or past medication course.</summary>
public class Medication : Entity, IHasDogId
{
    public int DogId { get; private set; }
    public Dog Dog { get; private set; } = null!;
    public string Name { get; private set; } = "";
    public string Dosage { get; private set; } = "";
    public string Frequency { get; private set; } = "";
    public DateTime StartDate { get; private set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Medication() { }

    public static Medication Create(
        int dogId, string name, string dosage, string frequency,
        DateTime startDate, DateTime? endDate) => new()
    {
        DogId = dogId,
        Name = name,
        Dosage = dosage,
        Frequency = frequency,
        StartDate = startDate,
        EndDate = endDate
    };

    public void Update(
        string name, string dosage, string frequency,
        DateTime startDate, DateTime? endDate, bool isActive)
    {
        Name = name;
        Dosage = dosage;
        Frequency = frequency;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;
}
