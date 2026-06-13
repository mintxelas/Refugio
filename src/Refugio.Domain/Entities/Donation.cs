using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

/// <summary>Aggregate root for a received donation.</summary>
public class Donation : Entity, IAggregateRoot
{
    public string DonorName { get; private set; } = "";
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; } = DateTime.UtcNow;
    public DonationCategory Category { get; private set; } = DonationCategory.OneTime;
    public string? Notes { get; private set; }
    public string? TaxId { get; private set; }

    private Donation() { }

    public static Donation Record(
        string donorName, decimal amount, DonationCategory category,
        string? notes = null, DateTime? date = null, string? taxId = null) => new()
    {
        DonorName = donorName,
        Amount = amount,
        Category = category,
        Notes = notes,
        Date = date ?? DateTime.UtcNow,
        TaxId = taxId
    };

    public void Update(string donorName, decimal amount, DonationCategory category, string? notes, string? taxId)
    {
        DonorName = donorName;
        Amount = amount;
        Category = category;
        Notes = notes;
        TaxId = taxId;
    }
}

public enum DonationCategory { Monthly, OneTime, InKind, Corporate }
