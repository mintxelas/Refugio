using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

public class ExpenseTaxLine : Entity
{
    public int ExpenseId { get; private set; }
    public Expense? Expense { get; private set; }
    public decimal IvaPercent { get; private set; }
    public decimal Base { get; private set; }
    public decimal Importe { get; private set; }

    private ExpenseTaxLine() { }

    public static ExpenseTaxLine Create(int expenseId, decimal ivaPercent, decimal baseAmount, decimal importe)
    {
        var expected = Math.Round(ivaPercent / 100m * baseAmount, 2);
        if (expected != importe)
            throw new ArgumentException($"Importe {importe} does not match IvaPercent {ivaPercent}% × Base {baseAmount} = {expected}.");
        return new ExpenseTaxLine { ExpenseId = expenseId, IvaPercent = ivaPercent, Base = baseAmount, Importe = importe };
    }
}
