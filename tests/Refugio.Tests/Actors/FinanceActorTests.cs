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

    // ── Goals ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAllGoals_ReturnsEmpty_WhenNone()
    {
        var result = await _actor.Ask<List<Goal>>(new GetAllGoals(), TimeSpan.FromSeconds(5));
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllGoals_ReturnsAll()
    {
        await SeedAsync(db =>
        {
            db.Goals.Add(new Goal { Title = "Van", TargetAmount = 10000m, CurrentAmount = 0m });
            db.Goals.Add(new Goal { Title = "Heating", TargetAmount = 5000m, CurrentAmount = 2000m });
            return db;
        });
        var result = await _actor.Ask<List<Goal>>(new GetAllGoals(), TimeSpan.FromSeconds(5));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllGoals_OrdersDeadlineFirstThenCreatedDesc()
    {
        await SeedAsync(db =>
        {
            db.Goals.Add(new Goal { Title = "NoDeadline", TargetAmount = 100m, CurrentAmount = 0m, CreatedAt = DateTime.UtcNow.AddDays(-1) });
            db.Goals.Add(new Goal { Title = "HasDeadline", TargetAmount = 100m, CurrentAmount = 0m, Deadline = DateTime.UtcNow.AddMonths(3) });
            return db;
        });
        var result = await _actor.Ask<List<Goal>>(new GetAllGoals(), TimeSpan.FromSeconds(5));
        Assert.Equal("HasDeadline", result[0].Title);
        Assert.Equal("NoDeadline", result[1].Title);
    }

    [Fact]
    public async Task GetGoalById_ReturnsGoal_WhenFound()
    {
        var seeded = await SeedAsync(db =>
        {
            var g = new Goal { Title = "Rescue Van", TargetAmount = 45000m, CurrentAmount = 15000m, Description = "New transport" };
            db.Goals.Add(g);
            return g;
        });
        var result = await _actor.Ask<Goal?>(new GetGoalById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("Rescue Van", result.Title);
        Assert.Equal(45000m, result.TargetAmount);
        Assert.Equal(15000m, result.CurrentAmount);
        Assert.Equal("New transport", result.Description);
    }

    [Fact]
    public async Task GetGoalById_ReturnsNull_WhenNotFound()
    {
        var result = await _actor.Ask<Goal?>(new GetGoalById(99999), TimeSpan.FromSeconds(5));
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateGoal_CreatesAndReturns()
    {
        var deadline = new DateTime(2026, 12, 31);
        var result = await _actor.Ask<Goal>(
            new CreateGoal("Emergency Fund", "Reserve for surgeries", 20000m, 5000m, deadline),
            TimeSpan.FromSeconds(5));
        Assert.Equal("Emergency Fund", result.Title);
        Assert.Equal("Reserve for surgeries", result.Description);
        Assert.Equal(20000m, result.TargetAmount);
        Assert.Equal(5000m, result.CurrentAmount);
        Assert.Equal(deadline, result.Deadline);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateGoal_WithNoDeadline_SetsDeadlineNull()
    {
        var result = await _actor.Ask<Goal>(
            new CreateGoal("Open Goal", null, 1000m, 0m, null),
            TimeSpan.FromSeconds(5));
        Assert.Null(result.Deadline);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task UpdateGoal_UpdatesAllFields_WhenFound()
    {
        var seeded = await SeedAsync(db =>
        {
            var g = new Goal { Title = "Old Title", TargetAmount = 1000m, CurrentAmount = 0m };
            db.Goals.Add(g);
            return g;
        });
        var newDeadline = new DateTime(2027, 6, 1);
        var result = await _actor.Ask<Goal?>(
            new UpdateGoal(seeded.Id, "New Title", "New desc", 9999m, 500m, newDeadline),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("New Title", result.Title);
        Assert.Equal("New desc", result.Description);
        Assert.Equal(9999m, result.TargetAmount);
        Assert.Equal(500m, result.CurrentAmount);
        Assert.Equal(newDeadline, result.Deadline);
    }

    [Fact]
    public async Task UpdateGoal_ReturnsNull_WhenNotFound()
    {
        var result = await _actor.Ask<Goal?>(
            new UpdateGoal(99999, "X", null, 1m, 0m, null),
            TimeSpan.FromSeconds(5));
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateGoal_CanClearDeadline()
    {
        var seeded = await SeedAsync(db =>
        {
            var g = new Goal { Title = "Deadline Goal", TargetAmount = 500m, CurrentAmount = 0m, Deadline = new DateTime(2026, 1, 1) };
            db.Goals.Add(g);
            return g;
        });
        var result = await _actor.Ask<Goal?>(
            new UpdateGoal(seeded.Id, "Deadline Goal", null, 500m, 0m, null),
            TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Null(result.Deadline);
    }

    [Fact]
    public async Task DeleteGoal_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedAsync(db =>
        {
            var g = new Goal { Title = "ToDelete", TargetAmount = 100m, CurrentAmount = 0m };
            db.Goals.Add(g);
            return g;
        });
        var result = await _actor.Ask<bool>(new DeleteGoal(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteGoal_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new DeleteGoal(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteGoal_SoftDeletes_HiddenFromGetAll()
    {
        var seeded = await SeedAsync(db =>
        {
            var g = new Goal { Title = "SoftDel", TargetAmount = 100m, CurrentAmount = 0m };
            db.Goals.Add(g);
            return g;
        });
        await _actor.Ask<bool>(new DeleteGoal(seeded.Id), TimeSpan.FromSeconds(5));
        var remaining = await _actor.Ask<List<Goal>>(new GetAllGoals(), TimeSpan.FromSeconds(5));
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DeleteGoal_SoftDeletes_SetsDeletedAt()
    {
        var seeded = await SeedAsync(db =>
        {
            var g = new Goal { Title = "SoftDelCheck", TargetAmount = 100m, CurrentAmount = 0m };
            db.Goals.Add(g);
            return g;
        });
        await _actor.Ask<bool>(new DeleteGoal(seeded.Id), TimeSpan.FromSeconds(5));
        var inDb = await ReadDirectAsync<Goal>(seeded.Id);
        Assert.NotNull(inDb);
        Assert.NotNull(inDb.DeletedAt);
    }

    // ── GetDeletedGoals / RestoreGoal ──────────────────────────

    [Fact]
    public async Task GetDeletedGoals_ReturnsEmpty_WhenNoneDeleted()
    {
        await SeedAsync(db => { db.Goals.Add(new Goal { Title = "Live", TargetAmount = 100m, CurrentAmount = 0m }); return db; });
        var deleted = await _actor.Ask<List<Goal>>(new GetDeletedGoals(), TimeSpan.FromSeconds(5));
        Assert.Empty(deleted);
    }

    [Fact]
    public async Task GetDeletedGoals_ReturnsOnlyDeleted()
    {
        await SeedAsync(db =>
        {
            db.Goals.Add(new Goal { Title = "Live", TargetAmount = 100m, CurrentAmount = 0m });
            db.Goals.Add(new Goal { Title = "Gone", TargetAmount = 500m, CurrentAmount = 0m, DeletedAt = DateTime.UtcNow });
            return db;
        });
        var deleted = await _actor.Ask<List<Goal>>(new GetDeletedGoals(), TimeSpan.FromSeconds(5));
        Assert.Single(deleted);
        Assert.Equal("Gone", deleted[0].Title);
    }

    [Fact]
    public async Task RestoreGoal_ReturnsTrue_AndAppearsInActiveQuery()
    {
        var seeded = await SeedAsync(db =>
        {
            var g = new Goal { Title = "Revive", TargetAmount = 2000m, CurrentAmount = 500m, DeletedAt = DateTime.UtcNow };
            db.Goals.Add(g);
            return g;
        });
        var result = await _actor.Ask<bool>(new RestoreGoal(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.True(result);
        var goal = await _actor.Ask<Goal?>(new GetGoalById(seeded.Id), TimeSpan.FromSeconds(5));
        Assert.NotNull(goal);
        Assert.Null(goal.DeletedAt);
    }

    [Fact]
    public async Task RestoreGoal_ReturnsFalse_WhenNotFound()
    {
        var result = await _actor.Ask<bool>(new RestoreGoal(99999), TimeSpan.FromSeconds(5));
        Assert.False(result);
    }

    [Fact]
    public async Task RestoreGoal_RemovedFromDeletedList_AfterRestore()
    {
        var seeded = await SeedAsync(db =>
        {
            var g = new Goal { Title = "BackFromDead", TargetAmount = 750m, CurrentAmount = 0m, DeletedAt = DateTime.UtcNow };
            db.Goals.Add(g);
            return g;
        });
        await _actor.Ask<bool>(new RestoreGoal(seeded.Id), TimeSpan.FromSeconds(5));
        var deletedAfter = await _actor.Ask<List<Goal>>(new GetDeletedGoals(), TimeSpan.FromSeconds(5));
        Assert.Empty(deletedAfter);
    }

    // ── Expense receipt gallery ────────────────────────────────

    [Fact]
    public async Task AddExpensePhoto_AddsRow_AndReturnsPhoto()
    {
        var exp = await SeedAsync(db => { var e = new Expense { Description = "Receipt1", Amount = 50m, Category = ExpenseCategory.Supplies }; db.Expenses.Add(e); return e; });
        var photo = await _actor.Ask<ExpensePhoto?>(new AddExpensePhoto(exp.Id, "/expenses/a.jpg"), TimeSpan.FromSeconds(5));
        Assert.NotNull(photo);
        Assert.Equal("/expenses/a.jpg", photo!.Url);
        var photos = await _actor.Ask<List<ExpensePhoto>>(new GetExpensePhotos(exp.Id), TimeSpan.FromSeconds(5));
        Assert.Single(photos);
    }

    [Fact]
    public async Task AddExpensePhoto_ReturnsNull_WhenExpenseMissing()
    {
        var photo = await _actor.Ask<ExpensePhoto?>(new AddExpensePhoto(99999, "/expenses/x.jpg"), TimeSpan.FromSeconds(5));
        Assert.Null(photo);
    }

    [Fact]
    public async Task GetExpensePhotos_ReturnsAll_NewestFirst()
    {
        var exp = await SeedAsync(db => { var e = new Expense { Description = "Receipt2", Amount = 50m, Category = ExpenseCategory.Supplies }; db.Expenses.Add(e); return e; });
        await _actor.Ask<ExpensePhoto?>(new AddExpensePhoto(exp.Id, "/expenses/a.jpg"), TimeSpan.FromSeconds(5));
        await _actor.Ask<ExpensePhoto?>(new AddExpensePhoto(exp.Id, "/expenses/b.jpg"), TimeSpan.FromSeconds(5));
        var photos = await _actor.Ask<List<ExpensePhoto>>(new GetExpensePhotos(exp.Id), TimeSpan.FromSeconds(5));
        Assert.Equal(2, photos.Count);
    }

    [Fact]
    public async Task DeleteExpensePhoto_HardDeletes_AndReturnsUrl()
    {
        var exp = await SeedAsync(db => { var e = new Expense { Description = "Receipt3", Amount = 50m, Category = ExpenseCategory.Supplies }; db.Expenses.Add(e); return e; });
        var photo = await _actor.Ask<ExpensePhoto?>(new AddExpensePhoto(exp.Id, "/expenses/a.jpg"), TimeSpan.FromSeconds(5));
        var deletedUrl = await _actor.Ask<string?>(new DeleteExpensePhoto(photo!.Id), TimeSpan.FromSeconds(5));
        Assert.Equal("/expenses/a.jpg", deletedUrl);
        var photos = await _actor.Ask<List<ExpensePhoto>>(new GetExpensePhotos(exp.Id), TimeSpan.FromSeconds(5));
        Assert.Empty(photos);
    }

    [Fact]
    public async Task DeleteExpensePhoto_ReturnsNull_WhenNotFound()
    {
        var deletedUrl = await _actor.Ask<string?>(new DeleteExpensePhoto(99999), TimeSpan.FromSeconds(5));
        Assert.Null(deletedUrl);
    }
}
