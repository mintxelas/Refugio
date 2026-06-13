using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

/// <summary>
/// Aggregate root for the dog and its medical history, medications and photo gallery.
/// All state changes go through behavior methods; EF rehydrates via the private ctor.
/// </summary>
public class Dog : Entity, IAggregateRoot
{
    public string Name { get; private set; } = "";
    public string Breed { get; private set; } = "";
    public int AgeMonths { get; private set; }
    public string Gender { get; private set; } = "";
    public DogStatus Status { get; private set; } = DogStatus.Available;
    public string? PhotoUrl { get; private set; }
    public string? Traits { get; private set; }
    public string? Notes { get; private set; }
    public DateTime ArrivalDate { get; private set; } = DateTime.UtcNow;
    public decimal WeightKg { get; private set; }

    public ICollection<MedicalRecord> MedicalRecords { get; } = [];
    public ICollection<Medication> Medications { get; } = [];
    public ICollection<Adoption> Adoptions { get; } = [];
    public ICollection<DogPhoto> Photos { get; } = [];

    private Dog() { }

    public static Dog CheckIn(
        string name, string breed, int ageMonths, string gender, decimal weightKg,
        string? photoUrl = null, string? traits = null, string? notes = null,
        DogStatus status = DogStatus.Available, DateTime? arrivalDate = null) => new()
    {
        Name = name,
        Breed = breed,
        AgeMonths = ageMonths,
        Gender = gender,
        WeightKg = weightKg,
        PhotoUrl = photoUrl,
        Traits = traits,
        Notes = notes,
        Status = status,
        ArrivalDate = arrivalDate ?? DateTime.UtcNow
    };

    public void UpdateDetails(
        string name, string breed, int ageMonths, string gender, DogStatus status,
        decimal weightKg, string? photoUrl, string? traits, string? notes, DateTime arrivalDate)
    {
        Name = name;
        Breed = breed;
        AgeMonths = ageMonths;
        Gender = gender;
        Status = status;
        WeightKg = weightKg;
        PhotoUrl = photoUrl;
        Traits = traits;
        Notes = notes;
        ArrivalDate = arrivalDate;
    }

    public void SetPhotoUrl(string? photoUrl) => PhotoUrl = photoUrl;
}

public enum DogStatus
{
    Available,
    Adopted,
    Foster,
    Medical,
    Quarantine
}
