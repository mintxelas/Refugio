namespace Refugio.Actors.Messages;

// Finance-area actor messages (donations, expenses, goals).
// Record/Update commands reuse the request records from Application.Contracts.
public sealed record GetDonations;
public sealed record GetDonationsPaged(int Page, int PageSize);
public sealed record GetDonation(int Id);
public sealed record DeleteDonation(int Id);
public sealed record RestoreDonation(int Id);
public sealed record PurgeDonation(int Id);
public sealed record GetDeletedDonations;
public sealed record GetDeletedDonation(int Id);

public sealed record GetExpenses;
public sealed record GetExpensesPaged(int Page, int PageSize);
public sealed record GetExpense(int Id);
public sealed record DeleteExpense(int Id);
public sealed record RestoreExpense(int Id);
public sealed record PurgeExpense(int Id);
public sealed record GetDeletedExpenses;
public sealed record GetDeletedExpense(int Id);
public sealed record GetExpensePhotos(int ExpenseId);
public sealed record AddExpensePhoto(int ExpenseId, string Url);
public sealed record RemoveExpensePhoto(int PhotoId);

public sealed record GetGoals;
public sealed record GetGoal(int Id);
public sealed record DeleteGoal(int Id);
public sealed record RestoreGoal(int Id);
public sealed record PurgeGoal(int Id);
public sealed record GetDeletedGoals;
public sealed record GetDeletedGoal(int Id);
