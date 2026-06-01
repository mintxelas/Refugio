namespace Refugio.Domain.Entities;

public class ExpensePhoto : ISoftDeletable
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public Expense? Expense { get; set; }
    public string Url { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }
}
