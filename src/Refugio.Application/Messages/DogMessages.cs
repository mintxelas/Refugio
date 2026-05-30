using Refugio.Domain.Entities;

namespace Refugio.Application.Messages;

public record GetAllDogs(string? Search = null, DogStatus? Status = null) : IDogMessage;
public record GetDogsPaged(string? Search, DogStatus? Status, int Page, int PageSize = 20) : IDogMessage;
public record GetDogById(int Id) : IDogMessage;
public record CreateDog(string Name, string Breed, int AgeMonths, string Gender, decimal WeightKg, string? PhotoUrl, string? Traits, string? Notes) : IDogMessage;
public record UpdateDog(int Id, string Name, string Breed, int AgeMonths, string Gender, DogStatus Status, decimal WeightKg, string? PhotoUrl, string? Traits, string? Notes) : IDogMessage;
public record DeleteDog(int Id) : IDogMessage;
public record UpdateDogPhoto(int Id, string? PhotoUrl) : IDogMessage;

public record GetMedicalRecords(int DogId) : IDogMessage;
public record CreateMedicalRecord(int DogId, string VetName, string Diagnosis, string Treatment, string? Notes, DateTime? NextVisitDate) : IDogMessage;

public record GetMedicalRecordById(int Id) : IDogMessage;
public record UpdateMedicalRecord(int Id, string VetName, string Diagnosis, string Treatment, string? Notes, DateTime VisitDate, DateTime? NextVisitDate) : IDogMessage;
public record DeleteMedicalRecord(int Id) : IDogMessage;

public record GetMedications(int DogId) : IDogMessage;
public record GetMedicationById(int Id) : IDogMessage;
public record CreateMedication(int DogId, string Name, string Dosage, string Frequency, DateTime StartDate, DateTime? EndDate) : IDogMessage;
public record UpdateMedication(int Id, string Name, string Dosage, string Frequency, DateTime StartDate, DateTime? EndDate, bool IsActive) : IDogMessage;
public record DeleteMedication(int Id) : IDogMessage;
public record DeactivateMedication(int MedicationId) : IDogMessage;

public record GetDashboardStats() : IDogMessage;

public record GetDeletedDogs() : IDogMessage;
public record RestoreDog(int Id) : IDogMessage;
public record GetDeletedDogById(int Id) : IDogMessage;
public record PermanentDeleteDog(int Id) : IDogMessage;

public record GetDeletedMedicalRecords() : IDogMessage;
public record RestoreMedicalRecord(int Id) : IDogMessage;
public record GetDeletedMedicalRecordById(int Id) : IDogMessage;
public record PermanentDeleteMedicalRecord(int Id) : IDogMessage;

public record GetDeletedMedications() : IDogMessage;
public record RestoreMedication(int Id) : IDogMessage;
public record GetDeletedMedicationById(int Id) : IDogMessage;
public record PermanentDeleteMedication(int Id) : IDogMessage;
