namespace Refugio.Domain.Entities;

public class Donation
{
    public int Id { get; set; }
    public string DonorName { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public DonationCategory Category { get; set; } = DonationCategory.OneTime;
    public string? Notes { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public enum DonationCategory { Monthly, OneTime, InKind, Corporate }
