using Refugio.Domain.Entities;

namespace Refugio.Application.Messages;

public record GetAllAdoptions(AdoptionStatus? Status = null) : IAdoptionMessage;
public record GetAdoptionsPaged(AdoptionStatus? Status, int Page, int PageSize = 25) : IAdoptionMessage;
public record GetAdoptionById(int Id) : IAdoptionMessage;
public record CreateAdoption(int DogId, string ApplicantName, string? ApplicantEmail, string? ApplicantPhone, AdoptionType Type, string? Notes) : IAdoptionMessage;
public record UpdateAdoption(int Id, string ApplicantName, string? ApplicantEmail, string? ApplicantPhone, AdoptionType Type, AdoptionStatus Status, string? Notes) : IAdoptionMessage;
public record UpdateAdoptionStatus(int Id, AdoptionStatus NewStatus, string? Notes) : IAdoptionMessage;
public record DeleteAdoption(int Id) : IAdoptionMessage;

public record GetDeletedAdoptions() : IAdoptionMessage;
public record RestoreAdoption(int Id) : IAdoptionMessage;
public record GetDeletedAdoptionById(int Id) : IAdoptionMessage;
public record PermanentDeleteAdoption(int Id) : IAdoptionMessage;

public record GetAdoptionConversionStats(int Year) : IAdoptionMessage;
public record AdoptionConversionStats(List<MonthlyConversionData> Monthly, int TotalApplied, int TotalFinalized);
public record MonthlyConversionData(int Month, int Applied, int Finalized);

public record GetShelterStayStats() : IAdoptionMessage;
public record ShelterStayStats(List<BreedStayData> ByBreed);
public record BreedStayData(string Breed, double AvgDays, int Count);
