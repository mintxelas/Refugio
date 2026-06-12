using Refugio.Domain.Entities;

namespace Refugio.Actors.Messages;

// Dog-aggregate actor messages (dogs, photo gallery, medical records, medications).
// Create/Update commands reuse the request records from Application.Contracts.
public sealed record GetDogs(string? Search, DogStatus? Status);
public sealed record GetDogsPaged(string? Search, DogStatus? Status, int Page, int PageSize);
public sealed record GetDog(int Id);
public sealed record DeleteDog(int Id);
public sealed record RestoreDog(int Id);
public sealed record PurgeDog(int Id);
public sealed record GetDeletedDogs;
public sealed record GetDeletedDog(int Id);

public sealed record SetDogPhoto(int Id, string? PhotoUrl);
public sealed record GetDogPhotos(int DogId);
public sealed record AddDogPhoto(int DogId, string Url);
public sealed record SetDefaultDogPhoto(int PhotoId);
public sealed record RemoveDogPhoto(int PhotoId);

public sealed record GetMedicalRecords(int DogId);
public sealed record GetMedicalRecord(int Id);
public sealed record DeleteMedicalRecord(int Id);
public sealed record RestoreMedicalRecord(int Id);
public sealed record PurgeMedicalRecord(int Id);
public sealed record GetDeletedMedicalRecords;
public sealed record GetDeletedMedicalRecord(int Id);

public sealed record GetMedications(int DogId);
public sealed record GetMedication(int Id);
public sealed record DeactivateMedication(int Id);
public sealed record DeleteMedication(int Id);
public sealed record RestoreMedication(int Id);
public sealed record PurgeMedication(int Id);
public sealed record GetDeletedMedications;
public sealed record GetDeletedMedication(int Id);
