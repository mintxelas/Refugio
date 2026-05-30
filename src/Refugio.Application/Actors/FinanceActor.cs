using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Infrastructure.Data;

namespace Refugio.Application.Actors;

public class FinanceActor : ReceiveActor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public FinanceActor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        ReceiveAsync<GetAllDonations>(Handle);
        ReceiveAsync<GetDonationsPaged>(Handle);
        ReceiveAsync<GetDonationById>(Handle);
        ReceiveAsync<CreateDonation>(Handle);
        ReceiveAsync<UpdateDonation>(Handle);
        ReceiveAsync<DeleteDonation>(Handle);
        ReceiveAsync<GetAllExpenses>(Handle);
        ReceiveAsync<GetExpensesPaged>(Handle);
        ReceiveAsync<GetExpenseById>(Handle);
        ReceiveAsync<CreateExpense>(Handle);
        ReceiveAsync<UpdateExpense>(Handle);
        ReceiveAsync<DeleteExpense>(Handle);
        ReceiveAsync<GetFinanceSummary>(Handle);
        ReceiveAsync<GetDeletedDonations>(Handle);
        ReceiveAsync<RestoreDonation>(Handle);
        ReceiveAsync<GetDeletedExpenses>(Handle);
        ReceiveAsync<RestoreExpense>(Handle);
    }

    private ShelterDbContext Db(IServiceScope s) => s.ServiceProvider.GetRequiredService<ShelterDbContext>();

    private async Task Handle(GetAllDonations msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Donations.OrderByDescending(d => d.Date).ToListAsync());
    }

    private async Task Handle(GetDonationsPaged msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var q = Db(scope).Donations.OrderByDescending(d => d.Date);
        var total = await q.CountAsync();
        var items = await q.Skip((msg.Page - 1) * msg.PageSize).Take(msg.PageSize).ToListAsync();
        Sender.Tell(new DonationPage(items, total, msg.Page, msg.PageSize));
    }

    private async Task Handle(GetDonationById msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Donations.FirstOrDefaultAsync(d => d.Id == msg.Id));
    }

    private async Task Handle(CreateDonation msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var donation = new Donation { DonorName = msg.DonorName, Amount = msg.Amount, Category = msg.Category, Notes = msg.Notes };
        db.Donations.Add(donation);
        await db.SaveChangesAsync();
        Sender.Tell(donation);
    }

    private async Task Handle(UpdateDonation msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var d = await db.Donations.FindAsync(msg.Id);
        if (d is null) { Sender.Tell((Donation?)null); return; }
        d.DonorName = msg.DonorName;
        d.Amount = msg.Amount;
        d.Category = msg.Category;
        d.Notes = msg.Notes;
        await db.SaveChangesAsync();
        Sender.Tell(d);
    }

    private async Task Handle(DeleteDonation msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var d = await db.Donations.FindAsync(msg.Id);
        if (d is null) { Sender.Tell(false); return; }
        d.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(GetAllExpenses msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Expenses.OrderByDescending(e => e.Date).ToListAsync());
    }

    private async Task Handle(GetExpensesPaged msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var q = Db(scope).Expenses.OrderByDescending(e => e.Date);
        var total = await q.CountAsync();
        var items = await q.Skip((msg.Page - 1) * msg.PageSize).Take(msg.PageSize).ToListAsync();
        Sender.Tell(new ExpensePage(items, total, msg.Page, msg.PageSize));
    }

    private async Task Handle(GetExpenseById msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Expenses.FirstOrDefaultAsync(e => e.Id == msg.Id));
    }

    private async Task Handle(CreateExpense msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var expense = new Expense { Description = msg.Description, Amount = msg.Amount, Category = msg.Category, Notes = msg.Notes };
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        Sender.Tell(expense);
    }

    private async Task Handle(UpdateExpense msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var e = await db.Expenses.FindAsync(msg.Id);
        if (e is null) { Sender.Tell((Expense?)null); return; }
        e.Description = msg.Description;
        e.Amount = msg.Amount;
        e.Category = msg.Category;
        e.Notes = msg.Notes;
        await db.SaveChangesAsync();
        Sender.Tell(e);
    }

    private async Task Handle(DeleteExpense msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var e = await db.Expenses.FindAsync(msg.Id);
        if (e is null) { Sender.Tell(false); return; }
        e.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(GetDeletedDonations msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Donations.IgnoreQueryFilters()
            .Where(d => d.DeletedAt != null)
            .OrderByDescending(d => d.DeletedAt)
            .ToListAsync());
    }

    private async Task Handle(RestoreDonation msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var d = await db.Donations.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == msg.Id);
        if (d is null) { Sender.Tell(false); return; }
        d.DeletedAt = null;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(GetDeletedExpenses msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Expenses.IgnoreQueryFilters()
            .Where(e => e.DeletedAt != null)
            .OrderByDescending(e => e.DeletedAt)
            .ToListAsync());
    }

    private async Task Handle(RestoreExpense msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var e = await db.Expenses.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == msg.Id);
        if (e is null) { Sender.Tell(false); return; }
        e.DeletedAt = null;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(GetFinanceSummary msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var donations = await db.Donations.Where(d => d.Date.Year == msg.Year).ToListAsync();
        var expenses = await db.Expenses.Where(e => e.Date.Year == msg.Year).ToListAsync();
        var monthly = Enumerable.Range(1, 12).Select(m => new MonthSummary(
            m,
            donations.Where(d => d.Date.Month == m).Sum(d => d.Amount),
            expenses.Where(e => e.Date.Month == m).Sum(e => e.Amount)
        )).ToList();
        Sender.Tell(new FinanceSummary(donations.Sum(d => d.Amount), expenses.Sum(e => e.Amount), monthly));
    }
}

public record MonthSummary(int Month, decimal Income, decimal Expenses);
public record FinanceSummary(decimal TotalIncome, decimal TotalExpenses, List<MonthSummary> Monthly);
