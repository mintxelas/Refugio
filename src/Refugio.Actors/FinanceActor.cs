using Microsoft.Extensions.DependencyInjection;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Services;

namespace Refugio.Actors;

/// <summary>Routes donations, expenses, and goals to IFinanceService.</summary>
public sealed class FinanceActor : ShelterActorBase<IFinanceService>
{
    public FinanceActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        // Donations
        Query<GetDonations>(async (s, _) => await s.GetDonationsAsync());
        Query<GetDonationsPaged>(async (s, m) => await s.GetDonationsPagedAsync(m.Page, m.PageSize));
        Query<GetDonation>(async (s, m) => await s.GetDonationAsync(m.Id));
        Query<GetDeletedDonations>(async (s, _) => await s.GetDeletedDonationsAsync());
        Query<GetDeletedDonation>(async (s, m) => await s.GetDeletedDonationAsync(m.Id));
        Command<CreateDonationRequest>(async (s, m) => await s.RecordDonationAsync(m));
        Command<UpdateDonationRequest>(async (s, m) => await s.UpdateDonationAsync(m));
        Command<DeleteDonation>(async (s, m) => await s.DeleteDonationAsync(m.Id));
        Command<RestoreDonation>(async (s, m) => await s.RestoreDonationAsync(m.Id));
        Command<PurgeDonation>(async (s, m) => await s.PurgeDonationAsync(m.Id));

        // Expenses
        Query<GetExpenses>(async (s, _) => await s.GetExpensesAsync());
        Query<GetExpensesPaged>(async (s, m) => await s.GetExpensesPagedAsync(m.Page, m.PageSize));
        Query<GetExpense>(async (s, m) => await s.GetExpenseAsync(m.Id));
        Query<GetDeletedExpenses>(async (s, _) => await s.GetDeletedExpensesAsync());
        Query<GetDeletedExpense>(async (s, m) => await s.GetDeletedExpenseAsync(m.Id));
        Command<CreateExpenseRequest>(async (s, m) => await s.RecordExpenseAsync(m));
        Command<UpdateExpenseRequest>(async (s, m) => await s.UpdateExpenseAsync(m));
        Command<DeleteExpense>(async (s, m) => await s.DeleteExpenseAsync(m.Id));
        Command<RestoreExpense>(async (s, m) => await s.RestoreExpenseAsync(m.Id));
        Command<PurgeExpense>(async (s, m) => await s.PurgeExpenseAsync(m.Id));
        Query<GetExpensePhotos>(async (s, m) => await s.GetExpensePhotosAsync(m.ExpenseId));
        Command<AddExpensePhoto>(async (s, m) => await s.AddExpensePhotoAsync(m.ExpenseId, m.Url));
        Command<RemoveExpensePhoto>(async (s, m) => await s.RemoveExpensePhotoAsync(m.PhotoId));

        // Goals
        Query<GetGoals>(async (s, _) => await s.GetGoalsAsync());
        Query<GetGoal>(async (s, m) => await s.GetGoalAsync(m.Id));
        Query<GetDeletedGoals>(async (s, _) => await s.GetDeletedGoalsAsync());
        Query<GetDeletedGoal>(async (s, m) => await s.GetDeletedGoalAsync(m.Id));
        Command<CreateGoalRequest>(async (s, m) => await s.CreateGoalAsync(m));
        Command<UpdateGoalRequest>(async (s, m) => await s.UpdateGoalAsync(m));
        Command<DeleteGoal>(async (s, m) => await s.DeleteGoalAsync(m.Id));
        Command<RestoreGoal>(async (s, m) => await s.RestoreGoalAsync(m.Id));
        Command<PurgeGoal>(async (s, m) => await s.PurgeGoalAsync(m.Id));
    }
}
