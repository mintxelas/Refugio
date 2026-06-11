using Refugio.Domain.Entities;

namespace Refugio.Application.Contracts;

public record DogDto(
    int Id, string Name, string Breed, int AgeMonths, string Gender, DogStatus Status,
    string? PhotoUrl, string? Traits, string? Notes, DateTime ArrivalDate, decimal WeightKg,
    DateTime? DeletedAt,
    List<MedicalRecordDto>? MedicalRecords = null,
    List<MedicationDto>? Medications = null,
    List<DogPhotoDto>? Photos = null);

public record MedicalRecordDto(
    int Id, int DogId, DateTime VisitDate, string VetName, string Diagnosis, string Treatment,
    string? Notes, DateTime? NextVisitDate, DateTime? DeletedAt, DogDto? Dog = null);

public record MedicationDto(
    int Id, int DogId, string Name, string Dosage, string Frequency,
    DateTime StartDate, DateTime? EndDate, bool IsActive, DateTime? DeletedAt, DogDto? Dog = null);

public record DogPhotoDto(int Id, int DogId, string Url, bool IsDefault, DateTime UploadedAt);

public record CreateDogRequest(
    string Name, string Breed, int AgeMonths, string Gender, decimal WeightKg,
    string? PhotoUrl, string? Traits, string? Notes);

public record UpdateDogRequest(
    int Id, string Name, string Breed, int AgeMonths, string Gender, DogStatus Status,
    decimal WeightKg, string? PhotoUrl, string? Traits, string? Notes);

public record CreateMedicalRecordRequest(
    int DogId, string VetName, string Diagnosis, string Treatment, string? Notes, DateTime? NextVisitDate);

public record UpdateMedicalRecordRequest(
    int Id, string VetName, string Diagnosis, string Treatment, string? Notes,
    DateTime VisitDate, DateTime? NextVisitDate);

public record CreateMedicationRequest(
    int DogId, string Name, string Dosage, string Frequency, DateTime StartDate, DateTime? EndDate);

public record UpdateMedicationRequest(
    int Id, string Name, string Dosage, string Frequency,
    DateTime StartDate, DateTime? EndDate, bool IsActive);
