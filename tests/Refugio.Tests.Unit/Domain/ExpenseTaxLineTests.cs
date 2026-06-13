using Refugio.Domain.Entities;

namespace Refugio.Tests.Domain;

public class ExpenseTaxLineTests
{
    // ── ExpenseTaxLine.Create ─────────────────────────────────────────────

    [Fact]
    public void Create_ValidTriplet_Succeeds()
    {
        var line = ExpenseTaxLine.Create(1, 21m, 100m, 21m);
        Assert.Equal(21m, line.IvaPercent);
        Assert.Equal(100m, line.Base);
        Assert.Equal(21m, line.Importe);
    }

    [Fact]
    public void Create_ZeroRate_WithZeroImporte_Succeeds()
    {
        var line = ExpenseTaxLine.Create(1, 0m, 50m, 0m);
        Assert.Equal(0m, line.Importe);
    }

    [Fact]
    public void Create_WrongImporte_Throws()
    {
        Assert.Throws<ArgumentException>(() => ExpenseTaxLine.Create(1, 21m, 100m, 22m));
    }

    [Fact]
    public void Create_SmallRoundingCase_Succeeds()
    {
        // 10% of 190.91 = 19.091 → rounded to 19.09
        var line = ExpenseTaxLine.Create(1, 10m, 190.91m, 19.09m);
        Assert.Equal(19.09m, line.Importe);
    }

    // ── Expense.SetTaxLines ───────────────────────────────────────────────

    [Fact]
    public void SetTaxLines_ZeroLines_Throws()
    {
        var expense = Expense.Record("X", 100m, ExpenseCategory.Other, [(21m, 100m, 21m)]);
        Assert.Throws<ArgumentException>(() => expense.SetTaxLines([]));
    }

    [Fact]
    public void SetTaxLines_SixLines_Throws()
    {
        var expense = Expense.Record("X", 100m, ExpenseCategory.Other, [(21m, 100m, 21m)]);
        var sixLines = Enumerable.Repeat((21m, 100m, 21m), 6);
        Assert.Throws<ArgumentException>(() => expense.SetTaxLines(sixLines));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void SetTaxLines_ValidCount_Succeeds(int count)
    {
        var expense = Expense.Record("X", 100m, ExpenseCategory.Other, [(21m, 100m, 21m)]);
        var lines = Enumerable.Repeat((21m, 100m, 21m), count);
        expense.SetTaxLines(lines);
        Assert.Equal(count, expense.TaxLines.Count);
    }
}
