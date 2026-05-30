using Refugio.Domain.Entities;

namespace Refugio.Application.Messages;

public record GetAllAdoptions(AdoptionStatus? Status = null);
public record GetAdoptionsPaged(AdoptionStatus? Status, int Page, int PageSize = 25);
public record AdoptionPage(List<Adoption> Items, int TotalCount, int Page, int PageSize);
public record GetAdoptionById(int Id);
public record CreateAdoption(int DogId, string ApplicantName, string? ApplicantEmail, string? ApplicantPhone, AdoptionType Type, string? Notes);
public record UpdateAdoption(int Id, string ApplicantName, string? ApplicantEmail, string? ApplicantPhone, AdoptionType Type, AdoptionStatus Status, string? Notes);
public record UpdateAdoptionStatus(int Id, AdoptionStatus NewStatus, string? Notes);
public record DeleteAdoption(int Id);

public record GetDeletedAdoptions();
public record RestoreAdoption(int Id);

public record GetAdoptionConversionStats(int Year);
public record AdoptionConversionStats(List<MonthlyConversionData> Monthly, int TotalApplied, int TotalFinalized);
public record MonthlyConversionData(int Month, int Applied, int Finalized);

public record GetShelterStayStats();
public record ShelterStayStats(List<BreedStayData> ByBreed);
public record BreedStayData(string Breed, double AvgDays, int Count);
