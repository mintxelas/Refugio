using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

/// <summary>Aggregate root for a shelter expense and its receipt photos.</summary>
public class Expense : Entity, IAggregateRoot
{
    public string Description { get; private set; } = "";
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; } = DateTime.UtcNow;
    public ExpenseCategory Category { get; private set; } = ExpenseCategory.Other;
    public string? Notes { get; private set; }

    public ICollection<ExpensePhoto> Photos { get; } = [];

    private Expense() { }

    public static Expense Record(
        string description, decimal amount, ExpenseCategory category,
        string? notes = null, DateTime? date = null) => new()
    {
        Description = description,
        Amount = amount,
        Category = category,
        Notes = notes,
        Date = date ?? DateTime.UtcNow
    };

    public void Update(string description, decimal amount, ExpenseCategory category, string? notes)
    {
        Description = description;
        Amount = amount;
        Category = category;
        Notes = notes;
    }
}

public enum ExpenseCategory { Medical, Food, Facilities, Supplies, Transport, Other }
