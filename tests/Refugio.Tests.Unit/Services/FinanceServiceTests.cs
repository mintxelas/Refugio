using Refugio.Application.Contracts;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Services;

public class FinanceServiceTests : ServiceTestBase
{
    private Task<T> Svc<T>(Func<IFinanceService, Task<T>> action) => WithServiceAsync(action);

    private Task<Donation> SeedDonation(string donor = "Alice", decimal amount = 100m,
        DonationCategory category = DonationCategory.OneTime, DateTime? date = null, DateTime? deletedAt = null)
        => SeedAsync(db =>
        {
            var donation = Donation.Record(donor, amount, category, date: date);
            donation.DeletedAt = deletedAt;
            db.Donations.Add(donation);
            return donation;
        });

    private Task<Expense> SeedExpense(string description = "Food", decimal amount = 100m,
        ExpenseCategory category = ExpenseCategory.Supplies, DateTime? date = null, DateTime? deletedAt = null)
        => SeedAsync(db =>
        {
            var expense = Expense.Record(description, amount, category, date: date);
            expense.DeletedAt = deletedAt;
            db.Expenses.Add(expense);
            return expense;
        });

    private Task<Goal> SeedGoal(string title = "Goal", decimal target = 100m, decimal current = 0m,
        DateTime? deadline = null, DateTime? createdAt = null, DateTime? deletedAt = null)
        => SeedAsync(db =>
        {
            var goal = Goal.Create(title, null, target, current, deadline, createdAt);
            goal.DeletedAt = deletedAt;
            db.Goals.Add(goal);
            return goal;
        });

    // ── Donations ──────────────────────────────────────────────

    [Fact]
    public async Task GetDonations_ReturnsEmpty_WhenNone()
    {
        Assert.Empty(await Svc(s => s.GetDonationsAsync()));
    }

    [Fact]
    public async Task GetDonations_ReturnsAll()
    {
        await SeedDonation("Alice");
        await SeedDonation("Bob", 50m, DonationCategory.Monthly);
        Assert.Equal(2, (await Svc(s => s.GetDonationsAsync())).Count);
    }

    [Fact]
    public async Task GetDonation_ReturnsDonation_WhenFound()
    {
        var seeded = await SeedDonation("Alice", 200m, DonationCategory.Corporate);
        var result = await Svc(s => s.GetDonationAsync(seeded.Id));
        Assert.NotNull(result);
        Assert.Equal("Alice", result.DonorName);
    }

    [Fact]
    public async Task RecordDonation_CreatesAndReturns()
    {
        var result = await Svc(s => s.RecordDonationAsync(
            new CreateDonationRequest("Corp", 500m, DonationCategory.Corporate, "annual donation")));
        Assert.Equal("Corp", result.DonorName);
        Assert.Equal(500m, result.Amount);
        Assert.Equal(DonationCategory.Corporate, result.Category);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task UpdateDonation_UpdatesFields_WhenFound()
    {
        var seeded = await SeedDonation("Old", 10m);
        var result = await Svc(s => s.UpdateDonationAsync(
            new UpdateDonationRequest(seeded.Id, "New", 999m, DonationCategory.InKind, "updated")));
        Assert.NotNull(result);
        Assert.Equal("New", result.DonorName);
        Assert.Equal(999m, result.Amount);
    }

    [Fact]
    public async Task UpdateDonation_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await Svc(s => s.UpdateDonationAsync(
            new UpdateDonationRequest(99999, "X", 1m, DonationCategory.OneTime, null))));
    }

    [Fact]
    public async Task DeleteDonation_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedDonation("Del", 1m);
        Assert.True(await Svc(s => s.DeleteDonationAsync(seeded.Id)));
    }

    [Fact]
    public async Task DeleteDonation_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.DeleteDonationAsync(99999)));
    }

    [Fact]
    public async Task GetDeletedDonations_ReturnsOnlyDeleted()
    {
        await SeedDonation("Live", 10m);
        await SeedDonation("Gone", 5m, DonationCategory.Monthly, deletedAt: DateTime.UtcNow);
        var deleted = await Svc(s => s.GetDeletedDonationsAsync());
        Assert.Single(deleted);
        Assert.Equal("Gone", deleted[0].DonorName);
    }

    [Fact]
    public async Task RestoreDonation_ReturnsTrue_AndAppearsInActiveQuery()
    {
        var seeded = await SeedDonation("Revive", 99m, DonationCategory.InKind, deletedAt: DateTime.UtcNow);
        Assert.True(await Svc(s => s.RestoreDonationAsync(seeded.Id)));
        var donation = await Svc(s => s.GetDonationAsync(seeded.Id));
        Assert.NotNull(donation);
        Assert.Null(donation.DeletedAt);
    }

    [Fact]
    public async Task RestoreDonation_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.RestoreDonationAsync(99999)));
    }

    [Fact]
    public async Task PurgeDonation_RemovesRow_OnlyWhenSoftDeleted()
    {
        var live = await SeedDonation("Live");
        var gone = await SeedDonation("Gone", deletedAt: DateTime.UtcNow);
        Assert.False(await Svc(s => s.PurgeDonationAsync(live.Id)));
        Assert.True(await Svc(s => s.PurgeDonationAsync(gone.Id)));
        Assert.Null(await ReadDirectAsync<Donation>(gone.Id));
    }

    // ── Expenses ───────────────────────────────────────────────

    [Fact]
    public async Task GetExpenses_ReturnsAll()
    {
        await SeedExpense("Food", 200m);
        await SeedExpense("Vet", 500m, ExpenseCategory.Medical);
        Assert.Equal(2, (await Svc(s => s.GetExpensesAsync())).Count);
    }

    [Fact]
    public async Task RecordExpense_CreatesAndReturns()
    {
        var result = await Svc(s => s.RecordExpenseAsync(
            new CreateExpenseRequest("Kennel Cleaning", 150m, ExpenseCategory.Facilities, "monthly clean")));
        Assert.Equal("Kennel Cleaning", result.Description);
        Assert.Equal(150m, result.Amount);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task UpdateExpense_UpdatesFields_WhenFound()
    {
        var seeded = await SeedExpense("Old", 10m, ExpenseCategory.Other);
        var result = await Svc(s => s.UpdateExpenseAsync(
            new UpdateExpenseRequest(seeded.Id, "New Desc", 999m, ExpenseCategory.Medical, null)));
        Assert.NotNull(result);
        Assert.Equal("New Desc", result.Description);
        Assert.Equal(999m, result.Amount);
    }

    [Fact]
    public async Task UpdateExpense_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await Svc(s => s.UpdateExpenseAsync(
            new UpdateExpenseRequest(99999, "X", 1m, ExpenseCategory.Other, null))));
    }

    [Fact]
    public async Task DeleteExpense_SoftDeletes_AndRestoreBringsBack()
    {
        var seeded = await SeedExpense("Cycle", 42m);
        Assert.True(await Svc(s => s.DeleteExpenseAsync(seeded.Id)));
        Assert.Null(await Svc(s => s.GetExpenseAsync(seeded.Id)));
        Assert.True(await Svc(s => s.RestoreExpenseAsync(seeded.Id)));
        Assert.NotNull(await Svc(s => s.GetExpenseAsync(seeded.Id)));
    }

    [Fact]
    public async Task GetDeletedExpenses_ReturnsOnlyDeleted()
    {
        await SeedExpense("Live", 10m, ExpenseCategory.Other);
        await SeedExpense("Gone", 5m, ExpenseCategory.Food, deletedAt: DateTime.UtcNow);
        var deleted = await Svc(s => s.GetDeletedExpensesAsync());
        Assert.Single(deleted);
        Assert.Equal("Gone", deleted[0].Description);
    }

    // ── Expense receipt gallery ────────────────────────────────

    [Fact]
    public async Task AddExpensePhoto_AddsRow_AndReturnsPhoto()
    {
        var expense = await SeedExpense("Receipt1", 50m);
        var photo = await Svc(s => s.AddExpensePhotoAsync(expense.Id, "/expenses/a.jpg"));
        Assert.NotNull(photo);
        Assert.Equal("/expenses/a.jpg", photo.Url);
        Assert.Single(await Svc(s => s.GetExpensePhotosAsync(expense.Id)));
    }

    [Fact]
    public async Task AddExpensePhoto_ReturnsNull_WhenExpenseMissing()
    {
        Assert.Null(await Svc(s => s.AddExpensePhotoAsync(99999, "/expenses/x.jpg")));
    }

    [Fact]
    public async Task GetExpensePhotos_ReturnsAll()
    {
        var expense = await SeedExpense("Receipt2", 50m);
        await Svc(s => s.AddExpensePhotoAsync(expense.Id, "/expenses/a.jpg"));
        await Svc(s => s.AddExpensePhotoAsync(expense.Id, "/expenses/b.jpg"));
        Assert.Equal(2, (await Svc(s => s.GetExpensePhotosAsync(expense.Id))).Count);
    }

    [Fact]
    public async Task RemoveExpensePhoto_HardDeletes_AndReturnsUrl()
    {
        var expense = await SeedExpense("Receipt3", 50m);
        var photo = await Svc(s => s.AddExpensePhotoAsync(expense.Id, "/expenses/a.jpg"));
        var deletedUrl = await Svc(s => s.RemoveExpensePhotoAsync(photo!.Id));
        Assert.Equal("/expenses/a.jpg", deletedUrl);
        Assert.Empty(await Svc(s => s.GetExpensePhotosAsync(expense.Id)));
    }

    [Fact]
    public async Task RemoveExpensePhoto_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await Svc(s => s.RemoveExpensePhotoAsync(99999)));
    }

    // ── Goals ──────────────────────────────────────────────────

    [Fact]
    public async Task GetGoals_ReturnsEmpty_WhenNone()
    {
        Assert.Empty(await Svc(s => s.GetGoalsAsync()));
    }

    [Fact]
    public async Task GetGoals_OrdersDeadlineFirstThenCreatedDesc()
    {
        await SeedGoal("NoDeadline", createdAt: DateTime.UtcNow.AddDays(-1));
        await SeedGoal("HasDeadline", deadline: DateTime.UtcNow.AddMonths(3));
        var result = await Svc(s => s.GetGoalsAsync());
        Assert.Equal("HasDeadline", result[0].Title);
        Assert.Equal("NoDeadline", result[1].Title);
    }

    [Fact]
    public async Task GetGoal_ReturnsGoal_WhenFound()
    {
        var seeded = await SeedGoal("Rescue Van", 45000m, 15000m);
        var result = await Svc(s => s.GetGoalAsync(seeded.Id));
        Assert.NotNull(result);
        Assert.Equal("Rescue Van", result.Title);
        Assert.Equal(45000m, result.TargetAmount);
        Assert.Equal(15000m, result.CurrentAmount);
    }

    [Fact]
    public async Task GetGoal_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await Svc(s => s.GetGoalAsync(99999)));
    }

    [Fact]
    public async Task CreateGoal_CreatesAndReturns()
    {
        var deadline = new DateTime(2026, 12, 31);
        var result = await Svc(s => s.CreateGoalAsync(
            new CreateGoalRequest("Emergency Fund", "Reserve for surgeries", 20000m, 5000m, deadline)));
        Assert.Equal("Emergency Fund", result.Title);
        Assert.Equal(20000m, result.TargetAmount);
        Assert.Equal(5000m, result.CurrentAmount);
        Assert.Equal(deadline, result.Deadline);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task UpdateGoal_UpdatesAllFields_AndCanClearDeadline()
    {
        var seeded = await SeedGoal("Old Title", 1000m, deadline: new DateTime(2026, 1, 1));
        var result = await Svc(s => s.UpdateGoalAsync(
            new UpdateGoalRequest(seeded.Id, "New Title", "New desc", 9999m, 500m, null)));
        Assert.NotNull(result);
        Assert.Equal("New Title", result.Title);
        Assert.Equal(9999m, result.TargetAmount);
        Assert.Null(result.Deadline);
    }

    [Fact]
    public async Task UpdateGoal_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await Svc(s => s.UpdateGoalAsync(new UpdateGoalRequest(99999, "X", null, 1m, 0m, null))));
    }

    [Fact]
    public async Task DeleteGoal_SoftDeletes_HiddenFromGetAll_AndSetsDeletedAt()
    {
        var seeded = await SeedGoal("SoftDel");
        Assert.True(await Svc(s => s.DeleteGoalAsync(seeded.Id)));
        Assert.Empty(await Svc(s => s.GetGoalsAsync()));
        var direct = await ReadDirectAsync<Goal>(seeded.Id);
        Assert.NotNull(direct!.DeletedAt);
    }

    [Fact]
    public async Task RestoreGoal_BringsBack_AndLeavesDeletedListEmpty()
    {
        var seeded = await SeedGoal("BackFromDead", 750m, deletedAt: DateTime.UtcNow);
        Assert.True(await Svc(s => s.RestoreGoalAsync(seeded.Id)));
        Assert.Empty(await Svc(s => s.GetDeletedGoalsAsync()));
        Assert.NotNull(await Svc(s => s.GetGoalAsync(seeded.Id)));
    }

    [Fact]
    public async Task RestoreGoal_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.RestoreGoalAsync(99999)));
    }
}
