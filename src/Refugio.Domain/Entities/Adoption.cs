namespace Refugio.Domain.Entities;

public class Adoption : ISoftDeletable
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public Dog Dog { get; set; } = null!;
    public string ApplicantName { get; set; } = "";
    public string? ApplicantEmail { get; set; }
    public string? ApplicantPhone { get; set; }
    public AdoptionType Type { get; set; } = AdoptionType.Adoption;
    public AdoptionStatus Status { get; set; } = AdoptionStatus.Applied;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
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
