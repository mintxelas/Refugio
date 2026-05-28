using Refugio.Domain.Entities;

namespace Refugio.Application.Messages;

public record GetAllAdoptions(AdoptionStatus? Status = null);
public record GetAdoptionById(int Id);
public record CreateAdoption(int DogId, string ApplicantName, string? ApplicantEmail, string? ApplicantPhone, AdoptionType Type, string? Notes);
public record UpdateAdoption(int Id, string ApplicantName, string? ApplicantEmail, string? ApplicantPhone, AdoptionType Type, AdoptionStatus Status, string? Notes);
public record UpdateAdoptionStatus(int Id, AdoptionStatus NewStatus, string? Notes);
public record DeleteAdoption(int Id);
