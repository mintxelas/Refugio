using Refugio.Domain.Common;
using Refugio.Domain.Events;

namespace Refugio.Domain.Entities;

/// <summary>
/// Aggregate root for an adoption or foster application moving through the pipeline.
/// Status changes via <see cref="ChangeStatus"/> raise <see cref="AdoptionStatusChanged"/>
/// (which notifies the applicant by email); <see cref="UpdateDetails"/> edits silently.
/// </summary>
public class Adoption : Entity, IAggregateRoot
{
    public int DogId { get; private set; }
    public Dog Dog { get; private set; } = null!;
    public string ApplicantName { get; private set; } = "";
    public string? ApplicantEmail { get; private set; }
    public string? ApplicantPhone { get; private set; }
    public AdoptionType Type { get; private set; } = AdoptionType.Adoption;
    public AdoptionStatus Status { get; private set; } = AdoptionStatus.Applied;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    private Adoption() { }

    public static Adoption Submit(
        int dogId, string applicantName, string? applicantEmail, string? applicantPhone,
        AdoptionType type, string? notes,
        AdoptionStatus status = AdoptionStatus.Applied, DateTime? createdAt = null) => new()
    {
        DogId = dogId,
        ApplicantName = applicantName,
        ApplicantEmail = applicantEmail,
        ApplicantPhone = applicantPhone,
        Type = type,
        Notes = notes,
        Status = status,
        CreatedAt = createdAt ?? DateTime.UtcNow
    };

    public void UpdateDetails(
        string applicantName, string? applicantEmail, string? applicantPhone,
        AdoptionType type, AdoptionStatus status, string? notes)
    {
        ApplicantName = applicantName;
        ApplicantEmail = applicantEmail;
        ApplicantPhone = applicantPhone;
        Type = type;
        Status = status;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(AdoptionStatus newStatus, string? notes = null)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        if (notes is not null) Notes = notes;
        Raise(new AdoptionStatusChanged(Id, ApplicantName, ApplicantEmail, newStatus));
    }

    /// <summary>The next step in the pipeline, or the current status when terminal.</summary>
    public AdoptionStatus NextStatus() => Status switch
    {
        AdoptionStatus.Applied   => AdoptionStatus.Interview,
        AdoptionStatus.Interview => AdoptionStatus.HomeCheck,
        AdoptionStatus.HomeCheck => AdoptionStatus.Approved,
        AdoptionStatus.Approved  => AdoptionStatus.Finalized,
        _                        => Status
    };
}

public enum AdoptionType { Adoption, Foster }

public enum AdoptionStatus
{
    Applied,
    Interview,
    HomeCheck,
    Approved,
    Finalized,
    Rejected
}
