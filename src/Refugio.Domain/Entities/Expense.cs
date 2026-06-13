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
    public ICollection<ExpenseTaxLine> TaxLines { get; private set; } = [];

    private Expense() { }

    public static Expense Record(
        string description, decimal amount, ExpenseCategory category,
        IEnumerable<(decimal IvaPercent, decimal Base, decimal Importe)> taxLines,
        string? notes = null, DateTime? date = null)
    {
        var expense = new Expense
        {
            Description = description,
            Amount = amount,
            Category = category,
            Notes = notes,
            Date = date ?? DateTime.UtcNow
        };
        expense.SetTaxLines(taxLines);
        return expense;
    }

    public void Update(string description, decimal amount, ExpenseCategory category, string? notes,
        IEnumerable<(decimal IvaPercent, decimal Base, decimal Importe)> taxLines)
    {
        Description = description;
        Amount = amount;
        Category = category;
        Notes = notes;
        SetTaxLines(taxLines);
    }

    public void SetTaxLines(IEnumerable<(decimal IvaPercent, decimal Base, decimal Importe)> lines)
    {
        var list = lines.ToList();
        if (list.Count < 1 || list.Count > 5)
            throw new ArgumentException("An expense must have between 1 and 5 tax lines.");
        TaxLines.Clear();
        foreach (var l in list)
            TaxLines.Add(ExpenseTaxLine.Create(Id, l.IvaPercent, l.Base, l.Importe));
    }
}

public enum ExpenseCategory { Medical, Food, Facilities, Supplies, Transport, Other }
