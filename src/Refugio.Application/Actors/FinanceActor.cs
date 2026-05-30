using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;

namespace Refugio.Application.Actors;

public class FinanceActor : ShelterActorBase
{
    public FinanceActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        ReceiveAsync<GetAllDonations>(Handle);
        ReceiveAsync<GetDonationsPaged>(Handle);
        ReceiveAsync<GetDonationById>(Handle);
        ReceiveAsync<CreateDonation>(Handle);
        ReceiveAsync<UpdateDonation>(Handle);
        ReceiveAsync<DeleteDonation>(msg => SoftDelete<Donation>(msg.Id));
        ReceiveAsync<GetAllExpenses>(Handle);
        ReceiveAsync<GetExpensesPaged>(Handle);
        ReceiveAsync<GetExpenseById>(Handle);
        ReceiveAsync<CreateExpense>(Handle);
        ReceiveAsync<UpdateExpense>(Handle);
        ReceiveAsync<DeleteExpense>(msg => SoftDelete<Expense>(msg.Id));
        ReceiveAsync<GetFinanceSummary>(Handle);
        ReceiveAsync<GetAllGoals>(Handle);
        ReceiveAsync<GetGoalById>(Handle);
        ReceiveAsync<CreateGoal>(Handle);
        ReceiveAsync<UpdateGoal>(Handle);
        ReceiveAsync<DeleteGoal>(msg => SoftDelete<Goal>(msg.Id));
        ReceiveAsync<GetDeletedGoals>(_ => GetDeleted<Goal>());
        ReceiveAsync<RestoreGoal>(msg => Restore<Goal>(msg.Id));
        ReceiveAsync<GetDeletedDonations>(_ => GetDeleted<Donation>());
        ReceiveAsync<RestoreDonation>(msg => Restore<Donation>(msg.Id));
        ReceiveAsync<GetDeletedExpenses>(_ => GetDeleted<Expense>());
        ReceiveAsync<RestoreExpense>(msg => Restore<Expense>(msg.Id));
    }

    private Task Handle(GetAllDonations msg) => WithDb(async db =>
        Sender.Tell(await db.Donations.OrderByDescending(d => d.Date).ToListAsync()));

    private Task Handle(GetDonationsPaged msg) => WithDb(async db =>
        Sender.Tell(await db.Donations.OrderByDescending(d => d.Date).ToPageAsync(msg.Page, msg.PageSize)));

    private Task Handle(GetDonationById msg) => WithDb(async db =>
        Sender.Tell(await db.Donations.FirstOrDefaultAsync(d => d.Id == msg.Id)));

    private Task Handle(CreateDonation msg) => WithDb(async db =>
    {
        var donation = new Donation { DonorName = msg.DonorName, Amount = msg.Amount, Category = msg.Category, Notes = msg.Notes };
        db.Donations.Add(donation);
        await db.SaveChangesAsync();
        Sender.Tell(donation);
    });

    private Task Handle(UpdateDonation msg) => WithDb(async db =>
    {
        var d = await db.Donations.FindAsync(msg.Id);
        if (d is null) { Sender.Tell((Donation?)null); return; }
        d.DonorName = msg.DonorName;
        d.Amount = msg.Amount;
        d.Category = msg.Category;
        d.Notes = msg.Notes;
        await db.SaveChangesAsync();
        Sender.Tell(d);
    });

    private Task Handle(GetAllExpenses msg) => WithDb(async db =>
        Sender.Tell(await db.Expenses.OrderByDescending(e => e.Date).ToListAsync()));

    private Task Handle(GetExpensesPaged msg) => WithDb(async db =>
        Sender.Tell(await db.Expenses.OrderByDescending(e => e.Date).ToPageAsync(msg.Page, msg.PageSize)));

    private Task Handle(GetExpenseById msg) => WithDb(async db =>
        Sender.Tell(await db.Expenses.FirstOrDefaultAsync(e => e.Id == msg.Id)));

    private Task Handle(CreateExpense msg) => WithDb(async db =>
    {
        var expense = new Expense { Description = msg.Description, Amount = msg.Amount, Category = msg.Category, Notes = msg.Notes };
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        Sender.Tell(expense);
    });

    private Task Handle(UpdateExpense msg) => WithDb(async db =>
    {
        var e = await db.Expenses.FindAsync(msg.Id);
        if (e is null) { Sender.Tell((Expense?)null); return; }
        e.Description = msg.Description;
        e.Amount = msg.Amount;
        e.Category = msg.Category;
        e.Notes = msg.Notes;
        await db.SaveChangesAsync();
        Sender.Tell(e);
    });

    private Task Handle(GetFinanceSummary msg) => WithDb(async db =>
    {
        var donations = await db.Donations.Where(d => d.Date.Year == msg.Year).ToListAsync();
        var expenses = await db.Expenses.Where(e => e.Date.Year == msg.Year).ToListAsync();
        var monthly = Enumerable.Range(1, 12).Select(m => new MonthSummary(
            m,
            donations.Where(d => d.Date.Month == m).Sum(d => d.Amount),
            expenses.Where(e => e.Date.Month == m).Sum(e => e.Amount)
        )).ToList();
        Sender.Tell(new FinanceSummary(donations.Sum(d => d.Amount), expenses.Sum(e => e.Amount), monthly));
    });

    private Task Handle(GetAllGoals msg) => WithDb(async db =>
        Sender.Tell(await db.Goals.OrderBy(g => g.Deadline == null).ThenBy(g => g.Deadline).ThenByDescending(g => g.CreatedAt).ToListAsync()));

    private Task Handle(GetGoalById msg) => WithDb(async db =>
        Sender.Tell(await db.Goals.FirstOrDefaultAsync(g => g.Id == msg.Id)));

    private Task Handle(CreateGoal msg) => WithDb(async db =>
    {
        var goal = new Goal { Title = msg.Title, Description = msg.Description, TargetAmount = msg.TargetAmount, CurrentAmount = msg.CurrentAmount, Deadline = msg.Deadline };
        db.Goals.Add(goal);
        await db.SaveChangesAsync();
        Sender.Tell(goal);
    });

    private Task Handle(UpdateGoal msg) => WithDb(async db =>
    {
        var g = await db.Goals.FindAsync(msg.Id);
        if (g is null) { Sender.Tell((Goal?)null); return; }
        g.Title = msg.Title;
        g.Description = msg.Description;
        g.TargetAmount = msg.TargetAmount;
        g.CurrentAmount = msg.CurrentAmount;
        g.Deadline = msg.Deadline;
        await db.SaveChangesAsync();
        Sender.Tell(g);
    });
}

public record MonthSummary(int Month, decimal Income, decimal Expenses);
public record FinanceSummary(decimal TotalIncome, decimal TotalExpenses, List<MonthSummary> Monthly);
