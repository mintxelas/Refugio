namespace Refugio.Domain.Entities;

public class Expense : ISoftDeletable
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public ExpenseCategory Category { get; set; } = ExpenseCategory.Other;
    public string? Notes { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public enum ExpenseCategory { Medical, Food, Facilities, Supplies, Transport, Other }
