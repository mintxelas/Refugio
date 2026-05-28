namespace Refugio.Domain.Entities;

public class Expense
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime? DeletedAt { get; set; }
}
