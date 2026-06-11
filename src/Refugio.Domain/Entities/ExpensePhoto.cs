using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

/// <summary>Child entity of the Expense aggregate: one receipt image.</summary>
public class ExpensePhoto : Entity
{
    public int ExpenseId { get; private set; }
    public Expense? Expense { get; private set; }
    public string Url { get; private set; } = "";
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    private ExpensePhoto() { }

    public static ExpensePhoto Create(int expenseId, string url) => new()
    {
        ExpenseId = expenseId,
        Url = url
    };
}
