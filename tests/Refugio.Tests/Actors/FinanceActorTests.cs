using Akka.Actor;
using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Actors;

public class FinanceActorTests : ActorTestBase
{
    private readonly IActorRef _actor;

    public FinanceActorTests()
    {
        _actor = Sys.ActorOf(Props.Create(() => new FinanceActor(_sf)));
    }

    // ── Donations ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllDonations_ReturnsEmpty_WhenNone()
    {
        var result = await _actor.Ask<List<Donation>>(new GetAllDonations(), TimeSpan.FromSeconds(5));
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllDonations_ReturnsAll()
    {
        await SeedAsync(db =>
        {
            db.Donations.Add(new Donation { DonorName = "Alice", Amount = 100m, Category = DonationCategory.OneTime });
            db.Donations.Add(new Donation { DonorName = "Bob", Amount = 50m, Category = DonationCategory.Monthly });
            return db;
        });
        var result = await _actor.Ask<List<Donation>>(new GetAllDonations(), TimeSpan.FromSeconds(5));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetDonationById_ReturnsDonation_WhenFound()
    {
        var seeded = await SeedAsync(db => { var d = new Donation { DonorName = "Alice", Amount = 200m, Category = DonationCategory.Corporate }; db.Donations.Add(d); return d; });
        var result = await _actor.Ask<Donation?>(new GetDonationById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("Alice", result.DonorName);
    }

    [Fact]
    public async Task CreateDonation_CreatesAndReturns()
    {
        var result = await _actor.Ask<Donation>(
            new CreateDonation("Corp", 500m, DonationCategory.Corporate, "annual donation"),
            TimeSpan.FromSeconds(5));
        Assert.Equal("Corp", result.DonorName);
        Assert.Equal(500m, result.Amount);
        Assert.Equal(DonationCategory.Corporate, result.Category);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task UpdateDonation_UpdatesDonation_WhenFound()
    {
        var seeded = await SeedAsync(db => { var d = new Donation { DonorName = "Old", Amount = 10m, Category = DonationCategory.OneTime }; db.Donations.Add(d); return d; });
        var result = await _actor.Ask<Donation?>(
            new UpdateDonation(seeded.Id, "New", 999m, DonationCategory.InKind, "updated"),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("New", result.DonorName);
        Assert.Equal(999m, result.Amount);
    }

    [Fact]
    public async Task UpdateDonation_ReturnsNull_WhenNotFound()
    {
        var result = await _actor.Ask<Donation?>(
            new UpdateDonation(99999, "X", 1m, DonationCategory.OneTime, null),
            TimeSpan.FromSeconds(5));
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteDonation_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedAsync(db => { var d = new Donation { DonorName = "Del", Amount = 1m, Category = DonationCategory.OneTime }; db.Donations.Add(d); return d; });
        var result = await _actor.Ask<bool>(new DeleteDonation(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteDonation_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new DeleteDonation(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    // ── Expenses ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllExpenses_ReturnsEmpty_WhenNone()
    {
        var result = await _actor.Ask<List<Expense>>(new GetAllExpenses(), TimeSpan.FromSeconds(5));
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllExpenses_ReturnsAll()
    {
        await SeedAsync(db =>
        {
            db.Expenses.Add(new Expense { Description = "Food", Amount = 200m, Category = ExpenseCategory.Supplies });
            db.Expenses.Add(new Expense { Description = "Vet", Amount = 500m, Category = ExpenseCategory.Medical });
            return db;
        });
        var result = await _actor.Ask<List<Expense>>(new GetAllExpenses(), TimeSpan.FromSeconds(5));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetExpenseById_ReturnsExpense_WhenFound()
    {
        var seeded = await SeedAsync(db => { var e = new Expense { Description = "Food", Amount = 100m, Category = ExpenseCategory.Supplies }; db.Expenses.Add(e); return e; });
        var result = await _actor.Ask<Expense?>(new GetExpenseById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("Food", result.Description);
    }

    [Fact]
    public async Task CreateExpense_CreatesAndReturns()
    {
        var result = await _actor.Ask<Expense>(
            new CreateExpense("Kennel Cleaning", 150m, ExpenseCategory.Facilities, "monthly clean"),
            TimeSpan.FromSeconds(5));
        Assert.Equal("Kennel Cleaning", result.Description);
        Assert.Equal(150m, result.Amount);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task UpdateExpense_UpdatesExpense_WhenFound()
    {
        var seeded = await SeedAsync(db => { var e = new Expense { Description = "Old", Amount = 10m, Category = ExpenseCategory.Other }; db.Expenses.Add(e); return e; });
        var result = await _actor.Ask<Expense?>(
            new UpdateExpense(seeded.Id, "New Desc", 999m, ExpenseCategory.Medical, null),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("New Desc", result.Description);
        Assert.Equal(999m, result.Amount);
    }

    [Fact]
    public async Task UpdateExpense_ReturnsNull_WhenNotFound()
    {
        var result = await _actor.Ask<Expense?>(
            new UpdateExpense(99999, "X", 1m, ExpenseCategory.Other, null),
            TimeSpan.FromSeconds(5));
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteExpense_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedAsync(db => { var e = new Expense { Description = "Del", Amount = 1m, Category = ExpenseCategory.Other }; db.Expenses.Add(e); return e; });
        var result = await _actor.Ask<bool>(new DeleteExpense(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteExpense_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new DeleteExpense(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    // ── Finance Summary ────────────────────────────────────────

    [Fact]
    public async Task GetFinanceSummary_ReturnsZeroes_WhenEmpty()
    {
        var result = await _actor.Ask<FinanceSummary>(new GetFinanceSummary(2024), TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(0m, result.TotalExpenses);
        Assert.Equal(12, result.Monthly.Count);
    }

    [Fact]
    public async Task GetFinanceSummary_SumsDonationsAndExpenses_ForYear()
    {
        var year = 2024;
        await SeedAsync(db =>
        {
            db.Donations.Add(new Donation { DonorName = "A", Amount = 300m, Category = DonationCategory.OneTime, Date = new DateTime(year, 3, 1) });
            db.Donations.Add(new Donation { DonorName = "B", Amount = 700m, Category = DonationCategory.Monthly, Date = new DateTime(year, 6, 1) });
            db.Expenses.Add(new Expense { Description = "Food", Amount = 200m, Category = ExpenseCategory.Supplies, Date = new DateTime(year, 3, 15) });
            return db;
        });
        var result = await _actor.Ask<FinanceSummary>(new GetFinanceSummary(year), TimeSpan.FromSeconds(5));
        Assert.Equal(1000m, result.TotalIncome);
        Assert.Equal(200m, result.TotalExpenses);
    }

    [Fact]
    public async Task GetFinanceSummary_IgnoresOtherYears()
    {
        await SeedAsync(db =>
        {
            db.Donations.Add(new Donation { DonorName = "A", Amount = 100m, Category = DonationCategory.OneTime, Date = new DateTime(2023, 1, 1) });
            return db;
        });
        var result = await _actor.Ask<FinanceSummary>(new GetFinanceSummary(2024), TimeSpan.FromSeconds(5));
        Assert.Equal(0m, result.TotalIncome);
    }

    [Fact]
    public async Task GetFinanceSummary_BreaksDownByMonth()
    {
        var year = 2025;
        await SeedAsync(db =>
        {
            db.Donations.Add(new Donation { DonorName = "X", Amount = 100m, Category = DonationCategory.OneTime, Date = new DateTime(year, 5, 1) });
            db.Expenses.Add(new Expense { Description = "Y", Amount = 50m, Category = ExpenseCategory.Other, Date = new DateTime(year, 5, 10) });
            return db;
        });
        var result = await _actor.Ask<FinanceSummary>(new GetFinanceSummary(year), TimeSpan.FromSeconds(5));
        var may = result.Monthly.First(m => m.Month == 5);
        Assert.Equal(100m, may.Income);
        Assert.Equal(50m, may.Expenses);
    }

    // ── GetDeletedDonations / RestoreDonation ──────────────────

    [Fact]
    public async Task GetDeletedDonations_ReturnsEmpty_WhenNoneDeleted()
    {
        await SeedAsync(db => { db.Donations.Add(new Donation { DonorName = "Live", Amount = 10m, Category = DonationCategory.OneTime }); return db; });
        var deleted = await _actor.Ask<List<Donation>>(new GetDeletedDonations(), TimeSpan.FromSeconds(5));
        Assert.Empty(deleted);
    }

    [Fact]
    public async Task GetDeletedDonations_ReturnsOnlyDeleted()
    {
        await SeedAsync(db =>
        {
            db.Donations.Add(new Donation { DonorName = "Live", Amount = 10m, Category = DonationCategory.OneTime });
            db.Donations.Add(new Donation { DonorName = "Gone", Amount = 5m, Category = DonationCategory.Monthly, DeletedAt = DateTime.UtcNow });
            return db;
        });
        var deleted = await _actor.Ask<List<Donation>>(new GetDeletedDonations(), TimeSpan.FromSeconds(5));
        Assert.Single(deleted);
        Assert.Equal("Gone", deleted[0].DonorName);
    }

    [Fact]
    public async Task RestoreDonation_ReturnsTrue_AndAppearsInActiveQuery()
    {
        var seeded = await SeedAsync(db =>
        {
            var d = new Donation { DonorName = "Revive", Amount = 99m, Category = DonationCategory.InKind, DeletedAt = DateTime.UtcNow };
            db.Donations.Add(d);
            return d;
        });
        var result = await _actor.Ask<bool>(new RestoreDonation(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
        var donation = await _actor.Ask<Donation?>(new GetDonationById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(donation);
        Assert.Null(donation.DeletedAt);
    }

    [Fact]
    public async Task RestoreDonation_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new RestoreDonation(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    // ── GetDeletedExpenses / RestoreExpense ────────────────────

    [Fact]
    public async Task GetDeletedExpenses_ReturnsEmpty_WhenNoneDeleted()
    {
        await SeedAsync(db => { db.Expenses.Add(new Expense { Description = "Live", Amount = 10m, Category = ExpenseCategory.Other }); return db; });
        var deleted = await _actor.Ask<List<Expense>>(new GetDeletedExpenses(), TimeSpan.FromSeconds(5));
        Assert.Empty(deleted);
    }

    [Fact]
    public async Task GetDeletedExpenses_ReturnsOnlyDeleted()
    {
        await SeedAsync(db =>
        {
            db.Expenses.Add(new Expense { Description = "Live", Amount = 10m, Category = ExpenseCategory.Other });
            db.Expenses.Add(new Expense { Description = "Gone", Amount = 5m, Category = ExpenseCategory.Food, DeletedAt = DateTime.UtcNow });
            return db;
        });
        var deleted = await _actor.Ask<List<Expense>>(new GetDeletedExpenses(), TimeSpan.FromSeconds(5));
        Assert.Single(deleted);
        Assert.Equal("Gone", deleted[0].Description);
    }

    [Fact]
    public async Task RestoreExpense_ReturnsTrue_AndAppearsInActiveQuery()
    {
        var seeded = await SeedAsync(db =>
        {
            var e = new Expense { Description = "ReviveExp", Amount = 42m, Category = ExpenseCategory.Transport, DeletedAt = DateTime.UtcNow };
            db.Expenses.Add(e);
            return e;
        });
        var result = await _actor.Ask<bool>(new RestoreExpense(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
        var expense = await _actor.Ask<Expense?>(new GetExpenseById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(expense);
        Assert.Null(expense.DeletedAt);
    }

    [Fact]
    public async Task RestoreExpense_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new RestoreExpense(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }
}
