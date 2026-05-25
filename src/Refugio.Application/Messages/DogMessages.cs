using Refugio.Domain.Entities;

namespace Refugio.Application.Messages;

public record GetAllDogs(string? Search = null, DogStatus? Status = null);
public record GetDogById(int Id);
public record CreateDog(string Name, string Breed, int AgeMonths, string Gender, decimal WeightKg, string? PhotoUrl, string? Traits, string? Notes);
public record UpdateDog(int Id, string Name, string Breed, int AgeMonths, string Gender, DogStatus Status, decimal WeightKg, string? PhotoUrl, string? Traits, string? Notes);
public record DeleteDog(int Id);

public record GetMedicalRecords(int DogId);
public record CreateMedicalRecord(int DogId, string VetName, string Diagnosis, string Treatment, string? Notes, DateTime? NextVisitDate);

public record GetMedications(int DogId);
public record CreateMedication(int DogId, string Name, string Dosage, string Frequency, DateTime StartDate, DateTime? EndDate);
public record DeactivateMedication(int MedicationId);

public record GetDashboardStats();
