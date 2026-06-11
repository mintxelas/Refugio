using Refugio.Domain.Common;
using Refugio.Domain.Entities;

namespace Refugio.Domain.Events;

/// <summary>Raised by <see cref="Adoption.ChangeStatus"/>; the handler emails the applicant.</summary>
public record AdoptionStatusChanged(
    int AdoptionId,
    string ApplicantName,
    string? ApplicantEmail,
    AdoptionStatus NewStatus) : IDomainEvent;
