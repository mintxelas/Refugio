using Refugio.Domain.Entities;

namespace Refugio.Application.Contracts;

public record AdoptionDto(
    int Id, int DogId, string ApplicantName, string? ApplicantEmail, string? ApplicantPhone,
    AdoptionType Type, AdoptionStatus Status, string? Notes,
    DateTime? PreAdoptionDate, DateTime? AdoptionDate,
    bool PreAdoptionFeeCharged, bool AdoptionFeeCharged,
    FeePaymentMethod? PreAdoptionFeePaymentMethod, FeePaymentMethod? AdoptionFeePaymentMethod,
    DateTime CreatedAt, DateTime? UpdatedAt, DateTime? DeletedAt,
    DogDto? Dog = null, List<AdoptionPhotoDto>? Photos = null);

public record AdoptionPhotoDto(int Id, int AdoptionId, string Url, DateTime UploadedAt);

public record CreateAdoptionRequest(
    int DogId, string ApplicantName, string? ApplicantEmail, string? ApplicantPhone,
    AdoptionType Type, string? Notes,
    DateTime? PreAdoptionDate = null, DateTime? AdoptionDate = null,
    bool PreAdoptionFeeCharged = false, bool AdoptionFeeCharged = false,
    FeePaymentMethod? PreAdoptionFeePaymentMethod = null, FeePaymentMethod? AdoptionFeePaymentMethod = null);

public record UpdateAdoptionRequest(
    int Id, string ApplicantName, string? ApplicantEmail, string? ApplicantPhone,
    AdoptionType Type, AdoptionStatus Status, string? Notes,
    DateTime? PreAdoptionDate = null, DateTime? AdoptionDate = null,
    bool PreAdoptionFeeCharged = false, bool AdoptionFeeCharged = false,
    FeePaymentMethod? PreAdoptionFeePaymentMethod = null, FeePaymentMethod? AdoptionFeePaymentMethod = null);

public record UpdateAdoptionStatusRequest(int Id, AdoptionStatus NewStatus, string? Notes);
